using Atelier.Web.Data;
using Atelier.Web.Data.Seed;
using Atelier.Web.Application.Auth;
using Atelier.Web.Application.Platform;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["ATELIER_SQLITE_CONNECTION_STRING"] ?? "Data Source=atelier.db";

builder.Services.AddDbContext<AtelierDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<EnterpriseWeChatAuthService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<UserBindingService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = EnterpriseWeChatAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = EnterpriseWeChatAuthenticationHandler.SchemeName;
    })
    .AddScheme<EnterpriseWeChatOAuthOptions, EnterpriseWeChatAuthenticationHandler>(EnterpriseWeChatAuthenticationHandler.SchemeName, options =>
    {
        options.ClientId = builder.Configuration["ATELIER_ENTERPRISE_WECHAT_CLIENT_ID"] ?? "corp-id";
        options.ClientSecret = builder.Configuration["ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET"] ?? string.Empty;
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();
    SeedData.EnsureSchemaAsync(dbContext).GetAwaiter().GetResult();
    SeedData.InitializeAsync(dbContext, app.Configuration).GetAwaiter().GetResult();
}

app.UseAuthentication();
app.Use(async (context, next) =>
{
    var authorizationService = context.RequestServices.GetRequiredService<AuthorizationService>();
    var requestPath = context.Request.Path;

    if (context.User.Identity?.IsAuthenticated == true
        && requestPath.StartsWithSegments("/Auth/WaitingForBinding", StringComparison.OrdinalIgnoreCase)
        && authorizationService.CanAccessBusinessPages(context.User))
    {
        context.Response.Redirect("/");
        return;
    }

    if (context.User.Identity?.IsAuthenticated == true
        && authorizationService.RequiresBinding(context.User)
        && !requestPath.StartsWithSegments("/Auth/WaitingForBinding", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect(authorizationService.BuildWaitingForBindingPath(context.User));
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapRazorPages();

app.Run();

public partial class Program;
