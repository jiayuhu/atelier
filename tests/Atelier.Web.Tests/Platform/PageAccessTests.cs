using System.Net;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Atelier.Web.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class PageAccessTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"atelier-page-access-{Guid.NewGuid():N}.db");
    private TestAppFactory? _factory;

    public async Task InitializeAsync()
    {
        _factory = TestAppFactory.ForDatabase(_databasePath);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();
        var workspace = await context.Workspaces.SingleAsync();
        var team = await context.Teams.FirstAsync(item => item.WorkspaceId == workspace.Id);

        if (!await context.Users.AnyAsync(user => user.EnterpriseWeChatUserId == "wx-member"))
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                TeamId = team.Id,
                EnterpriseWeChatUserId = "wx-member",
                DisplayName = "Member",
                Role = UserRole.Member,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await context.SaveChangesAsync();
        }
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WaitingForBinding_Page_RedirectsActiveUsersToHome()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/Auth/WaitingForBinding?enterpriseWeChatUserId=atelier-admin");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    [Fact]
    public async Task Settings_Page_ReturnsForbidden_ForNonAdministrators()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/Settings?enterpriseWeChatUserId=wx-member");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Settings_Page_RedirectsBindingRequiredUsers_ToWaitingForBinding()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/Settings?enterpriseWeChatUserId=wx-unbound");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be("/Auth/WaitingForBinding?enterpriseWeChatUserId=wx-unbound");
    }

    private HttpClient CreateClient()
    {
        return (_factory ?? throw new InvalidOperationException("Factory not initialized."))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
    }
}
