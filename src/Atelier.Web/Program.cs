using Atelier.Web.Data;
using Atelier.Web.Data.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetService<AtelierDbContext>();

    if (dbContext is null)
    {
        return;
    }

    SeedData.InitializeAsync(dbContext, app.Configuration).GetAwaiter().GetResult();
});

app.MapRazorPages();

app.Run();

public partial class Program;
