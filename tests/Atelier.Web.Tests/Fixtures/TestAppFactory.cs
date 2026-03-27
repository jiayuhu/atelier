using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Atelier.Web.Tests.Fixtures;

public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public TestAppFactory()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"atelier-tests-{Guid.NewGuid():N}.db");
    }

    private TestAppFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public static TestAppFactory ForDatabase(string databasePath) => new(databasePath);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ATELIER_SQLITE_CONNECTION_STRING"] = $"Data Source={_databasePath}",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
