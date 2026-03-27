using System.Net;
using Atelier.Web.Application.Auth;
using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Atelier.Web.Pages.Settings;
using Atelier.Web.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class EnterpriseWeChatAuthTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public EnterpriseWeChatAuthTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void MapIdentity_ReturnsBindingRequiredForUnknownUser()
    {
        var result = EnterpriseWeChatAuthService.MapIdentity("wx-user-1", knownUser: null);

        result.State.Should().Be(AuthBindingState.BindingRequired);
        result.UserId.Should().BeNull();
        result.Principal.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)!.Value.Should().Be("wx-user-1");
    }

    [Fact]
    public void MapIdentity_ReturnsActivePrincipalForKnownUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            EnterpriseWeChatUserId = "wx-user-1",
            DisplayName = "Casey",
            Role = UserRole.TeamLead,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = EnterpriseWeChatAuthService.MapIdentity("wx-user-1", user);

        result.State.Should().Be(AuthBindingState.Active);
        result.UserId.Should().Be(user.Id);
        result.Principal.Identity!.Name.Should().Be("Casey");
        result.Principal.IsInRole(nameof(UserRole.TeamLead)).Should().BeTrue();
    }

    [Fact]
    public async Task Challenge_RedirectsToEnterpriseWeChatAuthorizeEndpoint()
    {
        var result = await EnterpriseWeChatAuthService.BuildChallengeUrlAsync(
            clientId: "corp-id",
            redirectUri: "https://localhost/signin-wecom",
            state: "test-state");

        result.Should().StartWith("https://open.work.weixin.qq.com/wwopen/sso/qrConnect");
        result.Should().Contain("appid=corp-id");
        result.Should().Contain("signin-wecom");
        result.Should().Contain("state=test-state");
    }

    [Fact]
    public async Task Administrator_CanBindWaitingUserToInternalAccount()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AtelierDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Atelier",
            CreatedAt = createdAt,
        });

        context.Teams.Add(new Team
        {
            Id = teamId,
            WorkspaceId = workspaceId,
            Name = "Delivery",
            CreatedAt = createdAt,
        });

        context.Users.Add(new User
        {
            Id = userId,
            WorkspaceId = workspaceId,
            TeamId = teamId,
            EnterpriseWeChatUserId = "legacy-id",
            DisplayName = "Member",
            Role = UserRole.Member,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();

        var service = new UserBindingService(context);
        var binding = await service.BindAsync("wx-user-1", userId, actorRole: UserRole.Administrator);

        binding.State.Should().Be(AuthBindingState.Active);
        binding.UserId.Should().Be(userId);

        var user = await context.Users.SingleAsync(item => item.Id == userId);
        user.EnterpriseWeChatUserId.Should().Be("wx-user-1");
    }

    [Fact]
    public async Task WaitingForBinding_Page_ShowsPendingIdentityAndAdminBindingPath()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/Auth/WaitingForBinding?enterpriseWeChatUserId=wx-user-1");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("wx-user-1");
        html.Should().Contain("/Settings?enterpriseWeChatUserId=wx-user-1");
    }

    [Fact]
    public async Task SettingsPage_PrefillsPendingEnterpriseWeChatUserId()
    {
        await using var context = await CreateContextAsync();
        var model = new IndexModel(context, new UserBindingService(context));

        await model.OnGetAsync("wx-user-1");

        model.Input.EnterpriseWeChatUserId.Should().Be("wx-user-1");
    }

    [Fact]
    public async Task BindAsync_RejectsEmptyEnterpriseWeChatUserId()
    {
        await using var context = await CreateContextAsync();
        var userId = await SeedUserAsync(context, enterpriseWeChatUserId: "legacy-id");
        var service = new UserBindingService(context);

        var act = async () => await service.BindAsync("   ", userId, actorRole: UserRole.Administrator);

        await act.Should().ThrowAsync<UserBindingException>()
            .WithMessage("Enterprise WeChat user id is required.");
    }

    [Fact]
    public async Task BindAsync_RejectsEmptyUserSelection()
    {
        await using var context = await CreateContextAsync();
        var service = new UserBindingService(context);

        var act = async () => await service.BindAsync("wx-user-1", Guid.Empty, actorRole: UserRole.Administrator);

        await act.Should().ThrowAsync<UserBindingException>()
            .WithMessage("Select a user to bind.");
    }

    [Fact]
    public async Task BindAsync_RejectsDuplicateEnterpriseWeChatBinding()
    {
        await using var context = await CreateContextAsync();
        await SeedUserAsync(context, enterpriseWeChatUserId: "wx-user-1");
        var secondUserId = await SeedUserAsync(context, enterpriseWeChatUserId: "legacy-id-2", displayName: "Second Member");
        var service = new UserBindingService(context);

        var act = async () => await service.BindAsync("wx-user-1", secondUserId, actorRole: UserRole.Administrator);

        await act.Should().ThrowAsync<UserBindingException>()
            .WithMessage("Enterprise WeChat user id is already bound to another account.");
    }

    [Fact]
    public async Task UnmappedEnterpriseWeChatUser_IsRedirectedToWaitingForBindingAfterAuthentication()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/?enterpriseWeChatUserId=wx-user-1");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/Auth/WaitingForBinding");
        response.Headers.Location!.OriginalString.Should().Contain("enterpriseWeChatUserId=wx-user-1");
    }

    private static async Task<AtelierDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AtelierDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Guid> SeedUserAsync(AtelierDbContext context, string enterpriseWeChatUserId, string displayName = "Member")
    {
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = $"Workspace-{workspaceId:N}",
            CreatedAt = createdAt,
        });

        context.Teams.Add(new Team
        {
            Id = teamId,
            WorkspaceId = workspaceId,
            Name = $"Team-{teamId:N}",
            CreatedAt = createdAt,
        });

        context.Users.Add(new User
        {
            Id = userId,
            WorkspaceId = workspaceId,
            TeamId = teamId,
            EnterpriseWeChatUserId = enterpriseWeChatUserId,
            DisplayName = displayName,
            Role = UserRole.Member,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();
        return userId;
    }
}
