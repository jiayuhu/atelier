using Atelier.Web.Data;
using Atelier.Web.Data.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["ATELIER_SQLITE_CONNECTION_STRING"] ?? "Data Source=atelier.db";

builder.Services.AddDbContext<AtelierDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();
    SeedData.EnsureSchemaAsync(dbContext).GetAwaiter().GetResult();
    SeedData.InitializeAsync(dbContext, app.Configuration).GetAwaiter().GetResult();
}

app.MapRazorPages();

app.Run();

public partial class Program;
