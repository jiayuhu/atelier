using Atelier.Web.Data;
using Atelier.Web.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.Platform;

public static class StartupDatabaseInitializer
{
    public static async Task<bool> InitializeAsync(AtelierDbContext context, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        if (await HasExistingSchemaWithoutMigrationHistoryAsync(context, cancellationToken))
        {
            await SeedData.EnsureSchemaAsync(context, cancellationToken);
            await BackfillLegacyHolidayEntriesAsync(context, cancellationToken);
            await CompleteLegacySqliteMigrationAsync(context, cancellationToken);
            await SeedData.InitializeAsync(context, configuration, cancellationToken, backfillHolidays: false);
            return true;
        }

        await context.Database.MigrateAsync(cancellationToken);
        return false;
    }

    private static async Task BackfillLegacyHolidayEntriesAsync(AtelierDbContext context, CancellationToken cancellationToken)
    {
        var workspaceId = await context.Workspaces
            .Select(workspace => workspace.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId == Guid.Empty)
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var openedConnection = connection.State != System.Data.ConnectionState.Open;
        if (openedConnection)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            var workspaceIdValue = await ResolveStoredWorkspaceIdValueAsync(connection, workspaceId, cancellationToken);
            var blueprint = SeedData.BuildBlueprint();
            var now = DateTimeOffset.UtcNow;

            foreach (var holiday in blueprint.Holidays)
            {
                if (await HolidayEntryExistsAsync(connection, workspaceIdValue, holiday.Date, cancellationToken))
                {
                    continue;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = @"
INSERT INTO ""HolidayCalendarEntries"" (""Id"", ""WorkspaceId"", ""Date"", ""Name"", ""CreatedAt"")
VALUES ($id, $workspaceId, $date, $name, $createdAt);";

                AddParameter(command, "$id", Guid.NewGuid().ToString());
                AddParameter(command, "$workspaceId", workspaceIdValue);
                AddParameter(command, "$date", holiday.Date.ToString("yyyy-MM-dd"));
                AddParameter(command, "$name", holiday.Name);
                AddParameter(command, "$createdAt", now.ToString("O"));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (openedConnection)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task CompleteLegacySqliteMigrationAsync(AtelierDbContext context, CancellationToken cancellationToken)
    {
        var tempDatabasePath = Path.Combine(Path.GetTempPath(), $"atelier-startup-migrate-{Guid.NewGuid():N}.db");

        try
        {
            var tempOptions = new DbContextOptionsBuilder<AtelierDbContext>()
                .UseSqlite($"Data Source={tempDatabasePath}")
                .Options;

            await using (var tempContext = new AtelierDbContext(tempOptions))
            {
                await tempContext.Database.MigrateAsync(cancellationToken);
            }

            await using var tempConnection = new SqliteConnection($"Data Source={tempDatabasePath}");
            await tempConnection.OpenAsync(cancellationToken);

            var targetConnection = context.Database.GetDbConnection();
            var openedTargetConnection = targetConnection.State != System.Data.ConnectionState.Open;
            if (openedTargetConnection)
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
            }

            try
            {
                var schemaEntries = await ReadSchemaEntriesAsync(tempConnection, cancellationToken);
                foreach (var entry in schemaEntries)
                {
                    if (await ObjectExistsAsync(targetConnection, entry.Type, entry.Name, cancellationToken))
                    {
                        continue;
                    }

                    await using var command = targetConnection.CreateCommand();
                    command.CommandText = entry.Sql;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                var migrationHistory = await ReadMigrationHistoryAsync(tempConnection, cancellationToken);
                foreach (var row in migrationHistory)
                {
                    if (await MigrationHistoryRowExistsAsync(targetConnection, row.MigrationId, cancellationToken))
                    {
                        continue;
                    }

                    await using var insertCommand = targetConnection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($id, $version);";

                    var idParameter = insertCommand.CreateParameter();
                    idParameter.ParameterName = "$id";
                    idParameter.Value = row.MigrationId;
                    insertCommand.Parameters.Add(idParameter);

                    var versionParameter = insertCommand.CreateParameter();
                    versionParameter.ParameterName = "$version";
                    versionParameter.Value = row.ProductVersion;
                    insertCommand.Parameters.Add(versionParameter);

                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                if (openedTargetConnection)
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }
        finally
        {
            try
            {
                File.Delete(tempDatabasePath);
            }
            catch (IOException)
            {
            }
        }
    }

    private static async Task<bool> HasExistingSchemaWithoutMigrationHistoryAsync(AtelierDbContext context, CancellationToken cancellationToken)
    {
        var openedConnection = context.Database.GetDbConnection().State != System.Data.ConnectionState.Open;
        if (openedConnection)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            var hasWorkspacesTable = await TableExistsAsync(context, "Workspaces", cancellationToken);
            var hasMigrationHistory = await TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken);
            return hasWorkspacesTable && !hasMigrationHistory;
        }
        finally
        {
            if (openedConnection)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<bool> TableExistsAsync(AtelierDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task<bool> ObjectExistsAsync(System.Data.Common.DbConnection connection, string type, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = $type AND name = $name LIMIT 1;";

        var typeParameter = command.CreateParameter();
        typeParameter.ParameterName = "$type";
        typeParameter.Value = type;
        command.Parameters.Add(typeParameter);

        var nameParameter = command.CreateParameter();
        nameParameter.ParameterName = "$name";
        nameParameter.Value = name;
        command.Parameters.Add(nameParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task<bool> MigrationHistoryRowExistsAsync(System.Data.Common.DbConnection connection, string migrationId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $id LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$id";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task<object> ResolveStoredWorkspaceIdValueAsync(System.Data.Common.DbConnection connection, Guid workspaceId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ""Id""
FROM ""Workspaces""
WHERE ""Id"" = $workspaceId
LIMIT 1;";

        AddParameter(command, "$workspaceId", workspaceId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not null)
        {
            return result;
        }

        await using var fallbackCommand = connection.CreateCommand();
        fallbackCommand.CommandText = @"
SELECT ""Id""
FROM ""Workspaces""
ORDER BY rowid
LIMIT 1;";

        return await fallbackCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Expected a workspace row before holiday backfill.");
    }

    private static async Task<bool> HolidayEntryExistsAsync(System.Data.Common.DbConnection connection, object workspaceId, DateOnly date, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT 1
FROM ""HolidayCalendarEntries""
WHERE ""WorkspaceId"" = $workspaceId AND ""Date"" = $date
LIMIT 1;";

        AddParameter(command, "$workspaceId", workspaceId);
        AddParameter(command, "$date", date.ToString("yyyy-MM-dd"));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<IReadOnlyList<SchemaEntry>> ReadSchemaEntriesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT type, name, sql
FROM sqlite_master
WHERE sql IS NOT NULL
  AND name NOT LIKE 'sqlite_%'
  AND name != '__EFMigrationsLock'
ORDER BY CASE type WHEN 'table' THEN 0 WHEN 'index' THEN 1 ELSE 2 END, rowid;";

        var entries = new List<SchemaEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new SchemaEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return entries;
    }

    private static async Task<IReadOnlyList<MigrationHistoryRow>> ReadMigrationHistoryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";";

        var rows = new List<MigrationHistoryRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MigrationHistoryRow(
                reader.GetString(0),
                reader.GetString(1)));
        }

        return rows;
    }

    private sealed record SchemaEntry(string Type, string Name, string Sql);

    private sealed record MigrationHistoryRow(string MigrationId, string ProductVersion);
}
