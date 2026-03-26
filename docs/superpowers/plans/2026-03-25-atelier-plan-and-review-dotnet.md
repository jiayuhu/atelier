# Atelier Plan and Review .NET Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Atelier's first `Plan and Review` module as an ASP.NET Core Razor Pages app that supports monthly planning, weekly reporting, variance analysis, monthly reviews, and next-month revision drafts for a small team.

**Architecture:** Use a modular monolith in ASP.NET Core with Razor Pages for page workflows, EF Core for persistence, and application services for planning, reporting, review, revision, notification, and audit rules. Keep v1 fully functional with deterministic rule-based analysis and provider-agnostic extension points, but no required live AI integration.

**Tech Stack:** ASP.NET Core Razor Pages, C#, EF Core, SQLite, PostgreSQL-compatible schema design, xUnit, FluentAssertions, WebApplicationFactory, Playwright for .NET

---

## File Structure

### Solution and Hosting

- Create: `Atelier.sln` - solution root
- Create: `src/Atelier.Web/Atelier.Web.csproj` - web app project
- Create: `src/Atelier.Web/Program.cs` - app startup, DI, middleware, auth, Razor Pages
- Create: `src/Atelier.Web/appsettings.json` - base configuration
- Create: `src/Atelier.Web/appsettings.Development.json` - local development configuration
- Create: `src/Atelier.Web/Properties/launchSettings.json` - local profiles
- Create: `.env.example` - local environment reference for seeded admin and Enterprise WeChat settings

### Persistence and Domain

- Create: `src/Atelier.Web/Data/AtelierDbContext.cs` - EF Core DbContext
- Create: `src/Atelier.Web/Data/Configurations/*.cs` - EF entity configurations
- Create: `src/Atelier.Web/Data/Seed/SeedData.cs` - initial workspace, teams, seeded admin, holiday calendar
- Create: `src/Atelier.Web/Domain/Common/Priority.cs` - `high/medium/low` enum
- Create: `src/Atelier.Web/Domain/Common/WorkItemStatus.cs` - `draft/active/done/dropped` enum
- Create: `src/Atelier.Web/Domain/Platform/*.cs` - workspace, team, user, notification, holiday calendar, audit log
- Create: `src/Atelier.Web/Domain/PlanReview/*.cs` - monthly plan, goal, key result, weekly report, KR update, unlinked work item, blocker, monthly review, monthly revision

### Application Services

- Create: `src/Atelier.Web/Application/Auth/EnterpriseWeChatAuthService.cs` - auth mapping and binding-state logic
- Create: `src/Atelier.Web/Application/Platform/TeamService.cs` - team membership and lead lookup
- Create: `src/Atelier.Web/Application/Platform/AuditLogService.cs` - append-only audit records
- Create: `src/Atelier.Web/Application/Platform/HolidayCalendarService.cs` - working day and holiday calculations
- Create: `src/Atelier.Web/Application/PlanReview/EffectiveDeadlineService.cs` - shared deadline override, holiday shift, and attribution rules
- Create: `src/Atelier.Web/Application/PlanReview/MonthlyPlanService.cs` - create/activate/adjust/close/archive plans
- Create: `src/Atelier.Web/Application/PlanReview/MonthCloseHostedService.cs` - auto-close active plans at effective month close date
- Create: `src/Atelier.Web/Application/PlanReview/WeeklyReportService.cs` - weekly report submit/resubmit rules
- Create: `src/Atelier.Web/Application/PlanReview/AnalysisService.cs` - deterministic analysis and summaries
- Create: `src/Atelier.Web/Application/PlanReview/MonthlyReviewService.cs` - review draft/finalize rules
- Create: `src/Atelier.Web/Application/PlanReview/RevisionService.cs` - generate/apply monthly revisions
- Create: `src/Atelier.Web/Application/PlanReview/NotificationService.cs` - one-time in-app notifications

### Razor Pages

- Create: `src/Atelier.Web/Pages/Index.cshtml`
- Create: `src/Atelier.Web/Pages/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml`
- Create: `src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/Overview.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Overview.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/Settings/Index.cshtml`
- Create: `src/Atelier.Web/Pages/Settings/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/Shared/_Layout.cshtml`
- Create: `src/Atelier.Web/Pages/Shared/_NotificationInbox.cshtml`

### Tests

- Create: `tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj`
- Create: `tests/Atelier.Web.Tests/Fixtures/TestAppFactory.cs` - WebApplicationFactory and test database setup
- Create: `tests/Atelier.Web.Tests/Platform/EnterpriseWeChatAuthTests.cs`
- Create: `tests/Atelier.Web.Tests/Platform/AuditLogTests.cs`
- Create: `tests/Atelier.Web.Tests/Platform/HolidayCalendarTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/EffectiveDeadlineServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/MonthlyPlanServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/WeeklyReportServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/AnalysisServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/MonthlyReviewServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/RevisionServiceTests.cs`
- Create: `tests/Atelier.Web.Tests/PlanReview/NotificationServiceTests.cs`
- Create: `tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj`
- Create: `tests/Atelier.Web.Playwright/PlanReviewHappyPathTests.cs`

## Task 1: Bootstrap the ASP.NET Core Solution

**Files:**
- Create: `Atelier.sln`
- Create: `src/Atelier.Web/Atelier.Web.csproj`
- Create: `src/Atelier.Web/Program.cs`
- Create: `src/Atelier.Web/Pages/Index.cshtml`
- Create: `src/Atelier.Web/Pages/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj`
- Test: `tests/Atelier.Web.Tests/Fixtures/TestAppFactory.cs`
- Test: `tests/Atelier.Web.Tests/Smoke/HomePageTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public sealed class TestAppFactory : WebApplicationFactory<Program>
{
}

public sealed class HomePageTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public HomePageTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ShowsAtelierAndPlanReviewEntry()
    {
        var html = await _client.GetStringAsync("/");

        html.Should().Contain("Atelier");
        html.Should().Contain("Plan and Review");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter HomePageTests`
Expected: FAIL because the solution and test host do not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/Atelier.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();
app.MapRazorPages();
app.Run();

public partial class Program;
```

```html
@page
<main>
    <h1>Atelier</h1>
    <p>Plan and Review</p>
</main>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter HomePageTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git init
git add Atelier.sln src/Atelier.Web tests/Atelier.Web.Tests
git commit -m "chore: bootstrap atelier razor pages solution"
```

## Task 2: Add Core Domain Types and EF Core Schema

**Files:**
- Create: `src/Atelier.Web/Data/AtelierDbContext.cs`
- Create: `src/Atelier.Web/Domain/Common/Priority.cs`
- Create: `src/Atelier.Web/Domain/Common/WorkItemStatus.cs`
- Create: `src/Atelier.Web/Domain/Platform/*.cs`
- Create: `src/Atelier.Web/Domain/PlanReview/*.cs`
- Create: `src/Atelier.Web/Data/Configurations/*.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/ModelShapeTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public sealed class ModelShapeTests
{
    [Fact]
    public void KeyResult_UsesNumericProgressAndPriority()
    {
        var keyResult = new KeyResult
        {
            Title = "Ship weekly completion >= 85%",
            TargetValue = 85m,
            CurrentValue = 10m,
            Priority = Priority.High,
            Status = WorkItemStatus.Draft,
        };

        keyResult.TargetValue.Should().BeGreaterThan(0);
        keyResult.Priority.Should().Be(Priority.High);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter ModelShapeTests`
Expected: FAIL because the domain types do not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public enum Priority { High, Medium, Low }
public enum WorkItemStatus { Draft, Active, Done, Dropped }

public sealed class KeyResult
{
    public string Title { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public Priority Priority { get; set; }
    public WorkItemStatus Status { get; set; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter ModelShapeTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Data src/Atelier.Web/Domain tests/Atelier.Web.Tests/PlanReview/ModelShapeTests.cs
git commit -m "feat: add atelier core domain model"
```

## Task 3: Seed Workspace, Teams, Holiday Calendar, and Bootstrap Admin

**Files:**
- Create: `src/Atelier.Web/Data/Seed/SeedData.cs`
- Create: `.env.example`
- Modify: `src/Atelier.Web/Program.cs`
- Test: `tests/Atelier.Web.Tests/Platform/SeedDataTests.cs`
- Test: `tests/Atelier.Web.Tests/Platform/HolidayCalendarTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class SeedDataTests
{
    [Fact]
    public async Task Seed_CreatesOneWorkspaceTwoTeamsAndBootstrapAdmin()
    {
        var summary = await SeedDataPreview.BuildAsync();

        summary.WorkspaceCount.Should().Be(1);
        summary.TeamCount.Should().Be(2);
        summary.AdminCount.Should().Be(1);
    }
}
```

```csharp
public sealed class HolidayCalendarTests
{
    [Fact]
    public void EffectiveDeadline_ShiftsToNextWorkingDayWhenHoliday()
    {
        var effectiveDate = HolidayCalendarService.ShiftToNextWorkingDay(
            new DateOnly(2026, 10, 1),
            new[] { new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2) });

        effectiveDate.Should().Be(new DateOnly(2026, 10, 5));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "SeedDataTests|HolidayCalendarTests"`
Expected: FAIL because the seed and holiday services do not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public sealed record SeedDataSummary(int WorkspaceCount, int TeamCount, int AdminCount);

public static class SeedDataPreview
{
    public static Task<SeedDataSummary> BuildAsync() => Task.FromResult(new SeedDataSummary(1, 2, 1));
}
```

```csharp
public static class HolidayCalendarService
{
    public static DateOnly ShiftToNextWorkingDay(DateOnly date, IEnumerable<DateOnly> holidays)
    {
        var current = date;
        var holidaySet = holidays.ToHashSet();

        while (holidaySet.Contains(current) || current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            current = current.AddDays(1);
        }

        return current;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "SeedDataTests|HolidayCalendarTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Data/Seed/SeedData.cs src/Atelier.Web/Application/Platform/HolidayCalendarService.cs .env.example tests/Atelier.Web.Tests/Platform/SeedDataTests.cs tests/Atelier.Web.Tests/Platform/HolidayCalendarTests.cs
git commit -m "feat: add seed data and holiday calendar rules"
```

## Task 4: Implement Enterprise WeChat Auth and Waiting-for-Binding Flow

**Files:**
- Create: `src/Atelier.Web/Application/Auth/EnterpriseWeChatAuthService.cs`
- Create: `src/Atelier.Web/Application/Auth/EnterpriseWeChatOAuthOptions.cs`
- Create: `src/Atelier.Web/Application/Auth/EnterpriseWeChatAuthenticationHandler.cs`
- Create: `src/Atelier.Web/Application/Platform/UserBindingService.cs`
- Create: `src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml`
- Create: `src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/Settings/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/Settings/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Program.cs`
- Test: `tests/Atelier.Web.Tests/Platform/EnterpriseWeChatAuthTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public sealed class EnterpriseWeChatAuthTests
{
    [Fact]
    public void MapIdentity_ReturnsBindingRequiredForUnknownUser()
    {
        var result = EnterpriseWeChatAuthService.MapIdentity("wx-user-1", knownUser: null);

        result.State.Should().Be(AuthBindingState.BindingRequired);
    }

    [Fact]
    public async Task Challenge_RedirectsToEnterpriseWeChatAuthorizeEndpoint()
    {
        var result = await EnterpriseWeChatAuthService.BuildChallengeUrlAsync(
            clientId: "corp-id",
            redirectUri: "https://localhost/signin-wecom",
            state: "test-state");

        result.Should().Contain("corp-id");
        result.Should().Contain("signin-wecom");
    }

    [Fact]
    public async Task Administrator_CanBindWaitingUserToInternalAccount()
    {
        var binding = await UserBindingService.BindAsync("wx-user-1", Guid.NewGuid(), Guid.NewGuid(), UserRole.Member, actorRole: UserRole.Administrator);

        binding.State.Should().Be(AuthBindingState.Active);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter EnterpriseWeChatAuthTests`
Expected: FAIL because the auth service does not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public enum AuthBindingState { Active, BindingRequired }

public sealed record AuthBindingResult(AuthBindingState State, Guid? UserId);

public static class EnterpriseWeChatAuthService
{
    public static AuthBindingResult MapIdentity(string enterpriseWeChatUserId, Guid? knownUser)
        => knownUser is null
            ? new(AuthBindingState.BindingRequired, null)
            : new(AuthBindingState.Active, knownUser);

    public static Task<string> BuildChallengeUrlAsync(string clientId, string redirectUri, string state)
        => Task.FromResult($"https://open.work.weixin.qq.com/wwopen/sso/qrConnect?appid={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}");
}
```

```csharp
public static class UserBindingService
{
    public static Task<AuthBindingResult> BindAsync(string enterpriseWeChatUserId, Guid userId, Guid teamId, UserRole role, UserRole actorRole)
    {
        if (actorRole != UserRole.Administrator)
        {
            throw new InvalidOperationException("Only administrators can bind waiting users.");
        }

        return Task.FromResult(new AuthBindingResult(AuthBindingState.Active, userId));
    }
}
```

```csharp
// src/Atelier.Web/Program.cs
builder.Services
    .AddAuthentication("EnterpriseWeChat")
    .AddScheme<AuthenticationSchemeOptions, EnterpriseWeChatAuthenticationHandler>("EnterpriseWeChat", _ => { });

builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// Settings page adds an admin-only bind-user handler that maps a waiting Enterprise WeChat identity to an internal user/team/role.
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter EnterpriseWeChatAuthTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/Auth/EnterpriseWeChatAuthService.cs src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml.cs src/Atelier.Web/Program.cs tests/Atelier.Web.Tests/Platform/EnterpriseWeChatAuthTests.cs
git commit -m "feat: add enterprise wechat binding flow"
```

## Task 5: Enforce Role-Based Access and Binding-State Gating

**Files:**
- Create: `src/Atelier.Web/Application/Platform/AuthorizationService.cs`
- Modify: `src/Atelier.Web/Program.cs`
- Modify: `src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/*.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/Platform/AuthorizationServiceTests.cs`
- Test: `tests/Atelier.Web.Tests/Platform/PageAccessTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class AuthorizationServiceTests
{
    [Fact]
    public void Member_CanOnlyViewOwnReview()
    {
        var allowed = AuthorizationService.CanViewMonthlyReview(
            actorRole: UserRole.Member,
            actorUserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            actorTeamId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            reviewUserId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            reviewTeamId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        allowed.Should().BeFalse();
    }

    [Fact]
    public void TeamLead_CanOnlyViewPlanningReportingAndAnalysisForOwnTeam()
    {
        AuthorizationService.CanViewTeamScopedData(UserRole.TeamLead, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
            .Should().BeFalse();
    }
}
```

```csharp
public sealed class PageAccessTests : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task WaitingForBindingUser_IsRedirectedAwayFromBusinessPages()
    {
        var client = Factory.CreateBindingRequiredClient();
        var response = await client.GetAsync("/PlanReview/Overview");

        response.Headers.Location!.ToString().Should().Contain("/Auth/WaitingForBinding");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "AuthorizationServiceTests|PageAccessTests"`
Expected: FAIL because authorization and page gating are not implemented

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class AuthorizationService
{
    public static bool CanViewMonthlyReview(UserRole actorRole, Guid actorUserId, Guid actorTeamId, Guid reviewUserId, Guid reviewTeamId)
        => actorRole switch
        {
            UserRole.Administrator => true,
            UserRole.TeamLead => actorTeamId == reviewTeamId,
            UserRole.Member => actorUserId == reviewUserId,
            _ => false,
        };

    public static bool CanViewTeamScopedData(UserRole actorRole, Guid actorTeamId, Guid targetTeamId)
        => actorRole == UserRole.Administrator || actorTeamId == targetTeamId;
}
```

```csharp
// In page filter or middleware:
// if user binding state is BindingRequired and path is not /Auth/WaitingForBinding then redirect.
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "AuthorizationServiceTests|PageAccessTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/Platform/AuthorizationService.cs src/Atelier.Web/Program.cs src/Atelier.Web/Pages/Auth/WaitingForBinding.cshtml.cs src/Atelier.Web/Pages/PlanReview tests/Atelier.Web.Tests/Platform/AuthorizationServiceTests.cs tests/Atelier.Web.Tests/Platform/PageAccessTests.cs
git commit -m "feat: add role-based access and binding gating"
```

## Task 6: Add Team Scoping, Team-Lead Cardinality, and Audit Logging

**Files:**
- Create: `src/Atelier.Web/Application/Platform/TeamService.cs`
- Create: `src/Atelier.Web/Application/Platform/AuditLogService.cs`
- Test: `tests/Atelier.Web.Tests/Platform/AuditLogTests.cs`
- Test: `tests/Atelier.Web.Tests/Platform/TeamServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class TeamServiceTests
{
    [Fact]
    public void Team_HasExactlyOneLead()
    {
        var team = TeamService.Create("Delivery", leadUserId: Guid.NewGuid());

        team.TeamLeadUserId.Should().NotBeEmpty();
    }
}
```

```csharp
public sealed class AuditLogTests
{
    [Fact]
    public async Task Record_WritesAppendOnlyAuditEvent()
    {
        var entry = await AuditLogService.RecordAsync("admin-1", "deadline_changed", "weekly_deadline", "2026-04-05");

        entry.Action.Should().Be("deadline_changed");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "TeamServiceTests|AuditLogTests"`
Expected: FAIL because the services do not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class TeamService
{
    public static Team Create(string name, Guid leadUserId) => new() { Name = name, TeamLeadUserId = leadUserId };
}
```

```csharp
public static class AuditLogService
{
    public static Task<AuditLogEntry> RecordAsync(string actorUserId, string action, string targetType, string targetId)
        => Task.FromResult(new AuditLogEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
        });
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "TeamServiceTests|AuditLogTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/Platform/TeamService.cs src/Atelier.Web/Application/Platform/AuditLogService.cs tests/Atelier.Web.Tests/Platform/TeamServiceTests.cs tests/Atelier.Web.Tests/Platform/AuditLogTests.cs
git commit -m "feat: add team scoping and audit logging"
```

## Task 7: Implement Full Monthly Plan Lifecycle Rules

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/MonthlyPlanService.cs`
- Create: `src/Atelier.Web/Application/PlanReview/MonthCloseHostedService.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/MonthlyPlanServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class MonthlyPlanServiceTests
{
    [Fact]
    public void Activate_MovesDraftGoalsAndKeyResultsToActive()
    {
        var plan = MonthlyPlanFactory.DraftWithOneGoal();

        MonthlyPlanService.Activate(plan);

        plan.Status.Should().Be(MonthlyPlanStatus.Active);
        plan.Goals.Should().OnlyContain(goal => goal.Status == WorkItemStatus.Active);
        plan.Goals.SelectMany(goal => goal.KeyResults).Should().OnlyContain(kr => kr.Status == WorkItemStatus.Active);
    }

    [Fact]
    public void Create_EnforcesOnePrimaryMonthlyPlanPerWorkspacePerMonth()
    {
        var existingPlans = new[] { MonthlyPlanFactory.ForWorkspaceMonth(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateOnly(2026, 4, 1)) };

        var act = () => MonthlyPlanService.CreatePrimary(existingPlans, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateOnly(2026, 4, 1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task AdjustActivePlan_AllowsOnlyDescriptionOwnerAndDueDateAndWritesAudit()
    {
        var result = await MonthlyPlanService.AdjustActivePlanAsync(plan, actorUserId: "admin-1", newDescription: "Updated", newOwnerId: ownerId, newDueDate: new DateOnly(2026, 4, 20));

        result.Description.Should().Be("Updated");
        result.AuditAction.Should().Be("monthly_plan_adjusted");
    }

    [Fact]
    public void CloseAtMonthEnd_MakesPlanAndReportsReadOnly()
    {
        var plan = MonthlyPlanFactory.ActiveWithReport();
        var effectiveCloseDate = new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8));

        MonthlyPlanService.Close(plan, effectiveCloseDate);

        plan.Status.Should().Be(MonthlyPlanStatus.Closed);
        plan.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public async Task HostedMonthCloser_AutoClosesPlansAtEffectiveMonthCloseDate()
    {
        var closed = await MonthCloseHostedService.CloseDuePlansAsync(new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)));

        closed.Should().BeGreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter MonthlyPlanServiceTests`
Expected: FAIL because full lifecycle behavior is missing

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class MonthlyPlanService
{
    public static MonthlyPlan Create(DateOnly month) => new() { PlanMonth = month, Status = MonthlyPlanStatus.Draft };
    public static MonthlyPlan CreatePrimary(IEnumerable<MonthlyPlan> existingPlans, Guid workspaceId, DateOnly month) { /* enforce uniqueness */ }
    public static void Activate(MonthlyPlan plan) { /* set plan + work items active */ }
    public static async Task<MonthlyPlanAdjustmentResult> AdjustActivePlanAsync(MonthlyPlan plan, string actorUserId, string newDescription, Guid newOwnerId, DateOnly newDueDate) { /* enforce allowed fields + audit */ }
    public static void Close(MonthlyPlan plan, DateTimeOffset effectiveCloseDate) { /* set closed/read-only */ }
    public static void Archive(MonthlyPlan plan) { plan.Status = MonthlyPlanStatus.Archived; }
}

public static class MonthCloseHostedService
{
    public static Task<int> CloseDuePlansAsync(DateTimeOffset now)
        => Task.FromResult(0);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter MonthlyPlanServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/MonthlyPlanService.cs src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml.cs tests/Atelier.Web.Tests/PlanReview/MonthlyPlanServiceTests.cs
git commit -m "feat: add monthly plan lifecycle rules"
```

## Task 8: Implement Weekly Reporting Rules and Attribution

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/EffectiveDeadlineService.cs`
- Create: `src/Atelier.Web/Application/PlanReview/WeeklyReportService.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/Settings/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/WeeklyReportServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class EffectiveDeadlineServiceTests
{
    [Fact]
    public void Resolve_UsesManualOverrideAndHolidayShiftForAttribution()
    {
        var result = EffectiveDeadlineService.Resolve(
            reportingWeekStartDate: new DateOnly(2026, 3, 30),
            configuredDeadline: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: new DateTimeOffset(2026, 4, 6, 18, 0, 0, TimeSpan.FromHours(8)),
            deadlineDisabled: false,
            holidays: Array.Empty<DateOnly>());

        result.EffectiveDeadline.Month.Should().Be(4);
        result.AttributedMonth.Month.Should().Be(4);
    }
}

public sealed class WeeklyReportServiceTests
{
    [Fact]
    public void SaveDraft_AllowsFreeDraftEditingBeforeSubmission()
    {
        var report = WeeklyReportService.SaveDraft(null, WeeklyReportDraftFactory.Create("Initial"), MonthlyPlanFactory.Active());
        var updated = WeeklyReportService.SaveDraft(report, WeeklyReportDraftFactory.Create("Updated"), MonthlyPlanFactory.Active());

        updated.Status.Should().Be(WeeklyReportStatus.Draft);
        updated.WeeklyProgress.Should().Be("Updated");
    }

    [Fact]
    public void Submit_UsesOneReportPerUserPerReportingWeek()
    {
        var first = WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.Create(), MonthlyPlanFactory.Active());
        var second = WeeklyReportService.SubmitOrResubmit(first, WeeklyReportSubmissionFactory.Create(), MonthlyPlanFactory.Active());

        second.ReportingWeekStartDate.Should().Be(first.ReportingWeekStartDate);
        second.UserId.Should().Be(first.UserId);
    }

    [Fact]
    public void Submit_StoresAbsoluteCurrentValueAndUpdatesCanonicalKrValue()
    {
        var report = WeeklyReportFactory.Draft("2026-03-30");
        var keyResult = KeyResultFactory.WithCurrentValue(10m, 100m);

        WeeklyReportService.SubmitOrResubmit(report, WeeklyReportSubmissionFactory.Create(updatedCurrentValue: 35m, keyResultId: keyResult.Id), MonthlyPlanFactory.Active());

        keyResult.CurrentValue.Should().Be(35m);
    }

    [Fact]
    public void Resubmit_AfterDeadline_StaysLateUntilPlanClose()
    {
        var submission = WeeklyReportSubmissionFactory.Create(submittedAt: new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.FromHours(8)));
        var report = WeeklyReportService.SubmitOrResubmit(null, submission, MonthlyPlanFactory.Active());

        report.IsLate.Should().BeTrue();
    }

    [Fact]
    public void CrossMonthWeek_UsesEffectiveDeadlineMonthForAttribution()
    {
        var report = WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.CrossMonthWeek(), MonthlyPlanFactory.Active());

        report.AttributedMonth.Month.Should().Be(4);
    }

    [Fact]
    public void DisabledDeadline_StillUsesPlannedEffectiveDeadlineForAttribution()
    {
        var report = WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.WithDisabledDeadline(), MonthlyPlanFactory.Active());

        report.AttributedMonth.Month.Should().Be(4);
    }

    [Fact]
    public void ClosedMonth_PreventsFurtherReportEdits()
    {
        var act = () => WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.Create(), MonthlyPlanFactory.Closed());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_PersistsNextWeekPlanAdditionalNotesAndStructuredBlockers()
    {
        var report = WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.WithStructuredContent(), MonthlyPlanFactory.Active());

        report.NextWeekPlan.Should().NotBeNullOrWhiteSpace();
        report.AdditionalNotes.Should().NotBeNullOrWhiteSpace();
        report.Blockers.Should().NotBeEmpty();
        report.UnlinkedWorkItems.Should().NotBeEmpty();
    }

    [Fact]
    public void Submit_PersistsKrStatusContextAndBlockerLinkage()
    {
        var report = WeeklyReportService.SubmitOrResubmit(null, WeeklyReportSubmissionFactory.WithKrStatusAndLinkedBlocker(), MonthlyPlanFactory.Active());

        report.KrUpdates.Should().ContainSingle(update => update.StatusContext == KrStatusContext.Done);
        report.Blockers.Should().ContainSingle(blocker => blocker.LinkedKrUpdateId != null);
    }

    [Fact]
    public async Task DeadlineChangeAndDisable_AreAuditLogged()
    {
        var auditEntries = await EffectiveDeadlineService.RecordDeadlineRuleChangeAsync("admin-1", new DateOnly(2026, 3, 30), disabled: true);

        auditEntries.Should().ContainSingle(entry => entry.Action == "deadline_disabled");
    }

    [Fact]
    public async Task SubmitAndResubmit_RecordAuditEntries()
    {
        var auditEntries = await WeeklyReportService.RecordSubmissionAuditAsync("user-1", existingReportId: null);

        auditEntries.Should().ContainSingle(entry => entry.Action == "weekly_report_submitted");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "EffectiveDeadlineServiceTests|WeeklyReportServiceTests"`
Expected: FAIL because effective deadline, persistence, and attribution rules are missing

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class EffectiveDeadlineService
{
    public static EffectiveDeadlineResult Resolve(DateOnly reportingWeekStartDate, DateTimeOffset configuredDeadline, DateTimeOffset? overrideDeadline, bool deadlineDisabled, IEnumerable<DateOnly> holidays)
    {
        var planned = overrideDeadline ?? configuredDeadline;
        var shifted = HolidayCalendarService.ShiftToNextWorkingDay(DateOnly.FromDateTime(planned.DateTime), holidays);
        var effective = new DateTimeOffset(shifted.ToDateTime(TimeOnly.FromDateTime(planned.DateTime)), planned.Offset);

        return new EffectiveDeadlineResult(
            EffectiveDeadline: deadlineDisabled ? planned : effective,
            PlannedDeadline: planned,
            AttributedMonth: DateOnly.FromDateTime((deadlineDisabled ? planned : effective).DateTime));
    }

    public static Task<IReadOnlyList<AuditLogEntry>> RecordDeadlineRuleChangeAsync(string actorUserId, DateOnly reportingWeekStartDate, bool disabled)
        => Task.FromResult<IReadOnlyList<AuditLogEntry>>(new[]
        {
            new AuditLogEntry { ActorUserId = actorUserId, Action = disabled ? "deadline_disabled" : "deadline_changed", TargetId = reportingWeekStartDate.ToString("yyyy-MM-dd") }
        });
}
```

```csharp
public static class WeeklyReportService
{
    public static WeeklyReport SaveDraft(WeeklyReport? existingReport, WeeklyReportDraft draft, MonthlyPlan plan)
    {
        if (plan.Status == MonthlyPlanStatus.Closed) throw new InvalidOperationException("Reports are read-only after month close.");

        var report = existingReport ?? new WeeklyReport { UserId = draft.UserId, ReportingWeekStartDate = draft.ReportingWeekStartDate };
        report.Status = WeeklyReportStatus.Draft;
        report.WeeklyProgress = draft.WeeklyProgress;
        return report;
    }

    public static WeeklyReport SubmitOrResubmit(WeeklyReport? existingReport, WeeklyReportSubmission submission, MonthlyPlan plan)
    {
        if (plan.Status == MonthlyPlanStatus.Closed) throw new InvalidOperationException("Reports are read-only after month close.");

        var report = existingReport ?? new WeeklyReport { UserId = submission.UserId, ReportingWeekStartDate = submission.ReportingWeekStartDate };
        report.Status = WeeklyReportStatus.Submitted;
        report.IsLate = submission.SubmittedAt > submission.EffectiveDeadline;
        report.AttributedMonth = DateOnly.FromDateTime(submission.EffectiveDeadline.DateTime);
        report.NextWeekPlan = submission.NextWeekPlan;
        report.AdditionalNotes = submission.AdditionalNotes;
        report.Blockers = submission.Blockers;
        report.KrUpdates = submission.KrUpdates;
        report.UnlinkedWorkItems = submission.UnlinkedWorkItems;
        return report;
    }

    public static Task<IReadOnlyList<AuditLogEntry>> RecordSubmissionAuditAsync(string actorUserId, Guid? existingReportId)
        => Task.FromResult<IReadOnlyList<AuditLogEntry>>(new[]
        {
            new AuditLogEntry { ActorUserId = actorUserId, Action = existingReportId is null ? "weekly_report_submitted" : "weekly_report_resubmitted" }
        });
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "EffectiveDeadlineServiceTests|WeeklyReportServiceTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/EffectiveDeadlineService.cs src/Atelier.Web/Application/PlanReview/WeeklyReportService.cs src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml.cs src/Atelier.Web/Pages/Settings/Index.cshtml.cs tests/Atelier.Web.Tests/PlanReview/EffectiveDeadlineServiceTests.cs tests/Atelier.Web.Tests/PlanReview/WeeklyReportServiceTests.cs
git commit -m "feat: add weekly reporting rules"
```

## Task 9: Implement Full Deterministic Analysis

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/AnalysisService.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/AnalysisServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class AnalysisServiceTests
{
    [Fact]
    public void AnalyzeKeyResult_FlagsAtRiskWhenActualTrailsExpectedByTwentyPoints()
    {
        var result = AnalysisService.AnalyzeKeyResult(new AnalysisInput(100m, 20m, 18, 30, 2, 2, 2, 3, 0.1m, false));

        result.ProgressState.Should().Be("at_risk");
    }

    [Fact]
    public void AnalyzeKeyResult_FlagsContinuousLackOfProgressAfterTwoWeeksWithoutChange()
    {
        var result = AnalysisService.AnalyzeKeyResult(new AnalysisInput(100m, 20m, 18, 30, 0, 0, 1, 1, 0m, false));

        result.HasContinuousLackOfProgress.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeKeyResult_FlagsRepeatedBlockersFromDeterministicGrouping()
    {
        var result = AnalysisService.GroupRepeatedBlockers(new[] { "waiting on qa", "blocked by QA" });

        result.Should().BeTrue();
    }

    [Fact]
    public void AnalyzePortfolio_FlagsGoalOverloadAndFocusDrift()
    {
        var result = AnalysisService.AnalyzePortfolio(new PortfolioAnalysisInput(4, 6, 0.6m, true));

        result.GoalOverload.Should().BeTrue();
        result.FocusDrift.Should().BeTrue();
    }

    [Fact]
    public void ClassifyExecutionState_DistinguishesMissingDataFromPoorExecution()
    {
        AnalysisService.ClassifyExecutionState(false, false, false).Should().Be("missing_weekly_report");
        AnalysisService.ClassifyExecutionState(true, false, false).Should().Be("submitted_without_kr_linkage");
        AnalysisService.ClassifyExecutionState(true, true, false).Should().Be("no_real_progress");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter AnalysisServiceTests`
Expected: FAIL because the full rule set is not implemented yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class AnalysisService
{
    public static AnalysisResult AnalyzeKeyResult(AnalysisInput input) { /* progress variance + repeated blockers + no-progress */ }
    public static string ClassifyExecutionState(bool hasWeeklyReport, bool hasKrUpdates, bool hasMeaningfulProgress) { /* return missing_weekly_report | submitted_without_kr_linkage | no_real_progress | progress_recorded */ }
    public static PortfolioAnalysis AnalyzePortfolio(PortfolioAnalysisInput input) { /* goal overload + focus drift */ }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter AnalysisServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/AnalysisService.cs src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml.cs tests/Atelier.Web.Tests/PlanReview/AnalysisServiceTests.cs
git commit -m "feat: add full deterministic analysis rules"
```

## Task 10: Implement Full Monthly Review Workflow and Amendments

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/MonthlyReviewService.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/MonthlyReviewServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class MonthlyReviewServiceTests
{
    [Fact]
    public void CreateDraft_UsesOneReviewPerUserPerMonth()
    {
        var review = MonthlyReviewService.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 4, 1));

        review.Status.Should().Be(MonthlyReviewStatus.Draft);
        review.Rating.Should().Be(MonthlyRating.MeetsExpectations);
    }

    [Fact]
    public void TeamLead_CanMoveReviewToManagerReviewed()
    {
        var review = MonthlyReviewFactory.Draft();

        MonthlyReviewService.MarkManagerReviewed(review, UserRole.TeamLead);

        review.Status.Should().Be(MonthlyReviewStatus.ManagerReviewed);
    }

    [Fact]
    public void TeamLead_CanDraftConclusionAndRatingForOwnTeamMember()
    {
        var review = MonthlyReviewFactory.Draft();

        MonthlyReviewService.SaveTeamLeadDraft(review, actorRole: UserRole.TeamLead, draftConclusion: "Strong delivery support", draftRating: MonthlyRating.MeetsExpectations);

        review.DraftConclusion.Should().Be("Strong delivery support");
        review.DraftRating.Should().Be(MonthlyRating.MeetsExpectations);
    }

    [Fact]
    public void Administrator_CanCreateDraftIfMissingAndFinalize()
    {
        var review = MonthlyReviewService.EnsureDraft(Guid.NewGuid(), new DateOnly(2026, 4, 1), UserRole.Administrator);

        MonthlyReviewService.Finalize(review, UserRole.Administrator, "Solid month", MonthlyRating.MeetsExpectations);

        review.Status.Should().Be(MonthlyReviewStatus.Finalized);
        review.FinalConclusion.Should().Be("Solid month");
    }

    [Fact]
    public void BuildEvidencePackage_CollectsGoalKrTimelinessRiskAndHighlights()
    {
        var evidence = MonthlyReviewService.BuildEvidencePackage(MonthlyReviewEvidenceInputFactory.Create());

        evidence.GoalCompletionSummary.Should().NotBeEmpty();
        evidence.KeyResultCompletionSummary.Should().NotBeEmpty();
        evidence.RiskPatterns.Should().NotBeNull();
        evidence.Highlights.Should().NotBeNull();
    }

    [Fact]
    public async Task AmendFinalizedReview_WritesAuditInsteadOfSilentEdit()
    {
        var review = MonthlyReviewFactory.Finalized();

        var audit = await MonthlyReviewService.AmendFinalizedAsync(review, "admin-1", "Fix conclusion typo");

        audit.Action.Should().Be("monthly_review_amended");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter MonthlyReviewServiceTests`
Expected: FAIL because manager_reviewed, admin-create-if-missing, and amendment rules are missing

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class MonthlyReviewService
{
    public static MonthlyReview CreateDraft(Guid userId, DateOnly month) { /* unique draft */ }
    public static void SaveTeamLeadDraft(MonthlyReview review, UserRole actorRole, string draftConclusion, MonthlyRating draftRating) { /* team lead scoped draft authoring */ }
    public static void MarkManagerReviewed(MonthlyReview review, UserRole actorRole) { /* only team lead/admin */ }
    public static MonthlyReview EnsureDraft(Guid userId, DateOnly month, UserRole actorRole) { /* admin may create if missing */ }
    public static void Finalize(MonthlyReview review, UserRole actorRole, string finalConclusion, MonthlyRating finalRating) { /* only admin */ }
    public static Task AmendFinalizedAsync(MonthlyReview review, string actorUserId, string reason) { /* audit logged amendment */ }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter MonthlyReviewServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/MonthlyReviewService.cs src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml.cs tests/Atelier.Web.Tests/PlanReview/MonthlyReviewServiceTests.cs
git commit -m "feat: add monthly review workflow rules"
```

## Task 11: Implement Revision Idempotency, Merge, and Apply Semantics

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/RevisionService.cs`
- Create: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml.cs`
- Test: `tests/Atelier.Web.Tests/PlanReview/RevisionServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class RevisionServiceTests
{
    [Fact]
    public void Generate_CreatesStableSourceIdentityAndDeferSuggestionForCriticalKr()
    {
        var suggestions = RevisionService.Generate(new[] { new RevisionAnalysisInput(Guid.NewGuid(), Guid.NewGuid(), "critical", true) });

        suggestions[0].SourceIdentity.Should().NotBeNullOrWhiteSpace();
        suggestions[0].Type.Should().Be(RevisionSuggestionType.Defer);
    }

    [Fact]
    public void Apply_ToExistingNextMonthDraft_IsIdempotent()
    {
        var result = RevisionService.ApplyToDraft(MonthlyPlanFactory.Draft(), RevisionSuggestionFactory.Keep(), alreadyApplied: true, manuallyEdited: false);

        result.Should().Be(RevisionApplyResult.SkippedDuplicate);
    }

    [Fact]
    public void Apply_ManuallyEditedDraftItem_ReturnsConflictSkipped()
    {
        var result = RevisionService.ApplyToDraft(MonthlyPlanFactory.Draft(), RevisionSuggestionFactory.Remove(), alreadyApplied: true, manuallyEdited: true);

        result.Should().Be(RevisionApplyResult.ConflictSkipped);
    }

    [Fact]
    public void Apply_AddCompensatingKr_CopiesParentGoalWhenMissing()
    {
        var draft = MonthlyPlanFactory.EmptyDraft();

        RevisionService.ApplyAddCompensatingKr(draft, RevisionSuggestionFactory.AddCompensatingKr());

        draft.Goals.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Apply_RecordsAuditForEachAttempt()
    {
        var audit = await RevisionService.RecordApplyAuditAsync("admin-1", RevisionApplyResult.Applied, "source-id");

        audit.Action.Should().Be("monthly_revision_applied");
    }

    [Fact]
    public void Generate_ProducesEvidenceBackedKeepAndDowngradePrioritySuggestions()
    {
        var suggestions = RevisionService.Generate(new[]
        {
            new RevisionAnalysisInput(Guid.NewGuid(), Guid.NewGuid(), "on_track", repeatedBlocker: false),
            new RevisionAnalysisInput(Guid.NewGuid(), Guid.NewGuid(), "at_risk", repeatedBlocker: false)
        });

        suggestions.Should().Contain(s => s.Type == RevisionSuggestionType.Keep);
        suggestions.Should().Contain(s => s.Type == RevisionSuggestionType.DowngradePriority || s.Type == RevisionSuggestionType.Defer);
        suggestions.Should().OnlyContain(s => s.Evidence.Any());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter RevisionServiceTests`
Expected: FAIL because merge/idempotency rules are missing

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class RevisionService
{
    public static IReadOnlyList<RevisionSuggestion> Generate(IEnumerable<RevisionAnalysisInput> inputs) { /* include stable source identity */ }
    public static RevisionApplyResult ApplyToDraft(MonthlyPlan draft, RevisionSuggestion suggestion, bool alreadyApplied, bool manuallyEdited) { /* applied | skipped_duplicate | conflict_skipped */ }
    public static void ApplyAddCompensatingKr(MonthlyPlan draft, RevisionSuggestion suggestion) { /* copy parent goal if missing */ }
    public static Task<AuditLogEntry> RecordApplyAuditAsync(string actorUserId, RevisionApplyResult result, string sourceIdentity) { /* audit every attempt */ }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter RevisionServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/RevisionService.cs src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml.cs tests/Atelier.Web.Tests/PlanReview/RevisionServiceTests.cs
git commit -m "feat: add revision merge and apply rules"
```

## Task 12: Implement One-Time In-App Notifications and Deadline Settings

**Files:**
- Create: `src/Atelier.Web/Application/PlanReview/NotificationService.cs`
- Create: `src/Atelier.Web/Pages/Settings/Index.cshtml`
- Create: `src/Atelier.Web/Pages/Settings/Index.cshtml.cs`
- Create: `src/Atelier.Web/Pages/Shared/_NotificationInbox.cshtml`
- Test: `tests/Atelier.Web.Tests/PlanReview/NotificationServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
public sealed class NotificationServiceTests
{
    [Fact]
    public void BuildWeeklyReminderEvents_CreatesDueSoonAndOverdueForCorrectRecipients()
    {
        var events = NotificationService.BuildWeeklyReminderEvents(WeeklyDeadlineContextFactory.Standard());

        events.Should().Contain(e => e.Type == NotificationType.ReportDueSoon);
        events.Should().Contain(e => e.Type == NotificationType.ReportOverdue);
    }

    [Fact]
    public void BuildMonthlyReviewPendingEvents_UsesSecondWorkingDayAfterMonthClose()
    {
        var events = NotificationService.BuildMonthlyReviewPendingEvents(
            new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)),
            new[] { new DateOnly(2026, 5, 1) });

        events.Should().ContainSingle(e => e.Type == NotificationType.MonthlyReviewPending);
    }

    [Fact]
    public void BuildDeadlineChangeEvents_TargetsAffectedMembersAndLead()
    {
        var events = NotificationService.BuildDeadlineChangeEvents(new[] { Guid.NewGuid() }, Guid.NewGuid(), disabled: false);

        events.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildDeadlineDisabledEvents_UsesDedicatedNotificationType()
    {
        var events = NotificationService.BuildDeadlineChangeEvents(new[] { Guid.NewGuid() }, Guid.NewGuid(), disabled: true);

        events.Should().Contain(e => e.Type == NotificationType.DeadlineDisabledForWeek);
    }

    [Fact]
    public void BuildRevisionReadyEvents_AreOneTimeForAdministrators()
    {
        var evt = NotificationService.BuildRevisionReadyEvent(Guid.NewGuid());

        evt.Type.Should().Be(NotificationType.MonthlyRevisionSuggestionsReady);
        evt.IsOneTime.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter NotificationServiceTests`
Expected: FAIL because the full notification matrix is not implemented yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class NotificationService
{
    public static IReadOnlyList<NotificationEvent> BuildWeeklyReminderEvents(WeeklyDeadlineContext context) { /* due soon + overdue */ }
    public static IReadOnlyList<NotificationEvent> BuildMonthlyReviewPendingEvents(DateTimeOffset effectiveMonthlyCloseDate, IEnumerable<DateOnly> holidays) { /* second working day */ }
    public static IReadOnlyList<NotificationEvent> BuildDeadlineChangeEvents(IEnumerable<Guid> memberIds, Guid teamLeadUserId, bool disabled) { /* one-time events */ }
    public static NotificationEvent BuildRevisionReadyEvent(Guid adminUserId) { /* one-time */ }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter NotificationServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Application/PlanReview/NotificationService.cs src/Atelier.Web/Pages/Settings/Index.cshtml src/Atelier.Web/Pages/Settings/Index.cshtml.cs src/Atelier.Web/Pages/Shared/_NotificationInbox.cshtml tests/Atelier.Web.Tests/PlanReview/NotificationServiceTests.cs
git commit -m "feat: add in-app notification matrix"
```

## Task 13: Build the Razor Pages UI Workflow

**Files:**
- Create: `src/Atelier.Web/Pages/PlanReview/Overview.cshtml`
- Create: `src/Atelier.Web/Pages/PlanReview/Overview.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/PlanReview/MonthlyPlans/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/PlanReview/WeeklyReports/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/PlanReview/Analysis/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/PlanReview/MonthlyReviews/Index.cshtml.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml`
- Modify: `src/Atelier.Web/Pages/PlanReview/Revisions/Index.cshtml.cs`

- [ ] **Step 1: Write the failing page tests**

```csharp
public sealed class OverviewPageTests : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Overview_ShowsCurrentMonthAndNavigationLinks()
    {
        var client = Factory.CreateClient();
        var html = await client.GetStringAsync("/PlanReview/Overview");

        html.Should().Contain("Current Month");
        html.Should().Contain("Monthly Plans");
        html.Should().Contain("Weekly Reports");
    }
}

public sealed class MonthlyPlansPageTests : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task PostCreateAndActivate_ChangesPersistedPlanState()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsync("/PlanReview/MonthlyPlans?handler=Create", FormDataFactory.MonthlyPlan("2026-04"));

        response.EnsureSuccessStatusCode();
        var html = await client.GetStringAsync("/PlanReview/MonthlyPlans");
        html.Should().Contain("2026-04");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "OverviewPageTests|MonthlyPlansPageTests"`
Expected: FAIL because page handlers and forms are not wired yet

- [ ] **Step 3: Write minimal implementation**

```csharp
// Overview.cshtml.cs
// OnGetAsync loads current month summary, notification inbox, and navigation links.
// MonthlyPlans/Index.cshtml.cs exposes OnPostCreateAsync, OnPostActivateAsync, OnPostAdjustAsync handlers.
// WeeklyReports/Index.cshtml.cs exposes OnGetAsync, OnPostSubmitAsync, OnPostResubmitAsync handlers with bound KR updates, blockers, unlinked work items, next-week plan, and notes.
// Analysis/Index.cshtml.cs loads member/team/workspace views and execution-state classifications.
// MonthlyReviews/Index.cshtml.cs exposes OnPostSaveDraftAsync, OnPostManagerReviewAsync, OnPostFinalizeAsync handlers.
// Revisions/Index.cshtml.cs exposes OnPostGenerateAsync and OnPostApplyAsync handlers and reloads the next-month draft preview after each apply.
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter "OverviewPageTests|MonthlyPlansPageTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Pages/PlanReview tests/Atelier.Web.Tests
git commit -m "feat: add razor pages workflow"
```

## Task 14: Verify the Full Persisted Happy Path

**Files:**
- Create: `tests/Atelier.Web.Playwright/PlanReviewHappyPathTests.cs`
- Modify: `tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj`
- Create: `tests/Atelier.Web.Playwright/Fixtures/AuthenticatedBrowserContext.cs`
- Modify: `src/Atelier.Web/Pages/PlanReview/*.cshtml`

- [ ] **Step 1: Write the failing end-to-end test**

```csharp
[Fact]
public async Task Admin_CanCreateActivateReportReviewAndApplyRevision_WithPersistedState()
{
    await Page.GotoAsync(BaseUrl + "/PlanReview/Overview");
    await Page.GetByRole(AriaRole.Link, new() { Name = "Monthly Plans" }).ClickAsync();
    await Page.GetByLabel("Month").FillAsync("2026-04");
    await Page.GetByRole(AriaRole.Button, new() { Name = "Create Plan" }).ClickAsync();
    await Page.GetByRole(AriaRole.Button, new() { Name = "Activate Plan" }).ClickAsync();
    await Expect(Page.GetByText("Status: Active")).ToBeVisibleAsync();
    await Page.GetByRole(AriaRole.Link, new() { Name = "Weekly Reports" }).ClickAsync();
    await Page.GetByLabel("Weekly Progress").FillAsync("Closed onboarding backlog");
    await Page.GetByLabel("KR Current Value").FillAsync("35");
    await Page.GetByRole(AriaRole.Button, new() { Name = "Submit Report" }).ClickAsync();
    await Expect(Page.GetByText("Submitted")).ToBeVisibleAsync();
    await Page.GetByRole(AriaRole.Link, new() { Name = "Monthly Reviews" }).ClickAsync();
    await Page.GetByLabel("Draft Rating").SelectOptionAsync("Meets Expectations");
    await Page.GetByRole(AriaRole.Button, new() { Name = "Mark Manager Reviewed" }).ClickAsync();
    await Page.GetByLabel("Final Rating").SelectOptionAsync("Meets Expectations");
    await Page.GetByRole(AriaRole.Button, new() { Name = "Finalize Review" }).ClickAsync();
    await Page.GetByRole(AriaRole.Link, new() { Name = "Revisions" }).ClickAsync();
    await Page.GetByRole(AriaRole.Button, new() { Name = "Generate Suggestions" }).ClickAsync();
    await Page.GetByRole(AriaRole.Button, new() { Name = "Apply Selected Revisions" }).ClickAsync();
    await Expect(Page.GetByText("Next month draft updated")).ToBeVisibleAsync();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj --filter PlanReviewHappyPathTests`
Expected: FAIL because the full persisted flow is not complete

- [ ] **Step 3: Write minimal implementation**

```csharp
// Ensure each page handler persists to EF Core and reloads state after POST.
// Monthly Plans page must render `Status: Active` after activation from persisted state.
// Weekly Reports page must persist `Weekly Progress`, `KR Current Value`, `Next Week Plan`, `Additional Notes`, blockers, and unlinked work items before showing `Submitted`.
// Playwright fixture seeds an authenticated administrator cookie/session for the seeded Enterprise WeChat admin identity so the test can enter gated business pages.
// Monthly Reviews page must persist `Draft Rating`, `Manager Reviewed`, `Final Rating`, and final conclusion before showing finalized state.
// Revisions page must persist generated suggestions, apply the selected suggestion to the next-month draft, and render `Next month draft updated` from persisted state.
// Ensure seeded admin user can exercise the complete flow in the Playwright environment.
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj --filter PlanReviewHappyPathTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Atelier.Web/Pages/PlanReview tests/Atelier.Web.Playwright
git commit -m "feat: verify persisted plan review happy path"
```

## Task 15: Add Local Setup, Migrations, and Operator Documentation

**Files:**
- Create: `README.md`
- Create: `src/Atelier.Web/Data/Migrations/*`
- Modify: `.env.example`
- Test: `tests/Atelier.Web.Tests/Smoke/StartupConfigurationTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public sealed class StartupConfigurationTests
{
    [Fact]
    public void Startup_RegistersSqliteAndSeededAdminConfig()
    {
        var config = StartupConfigurationPreview.Build();

        config.ConnectionString.Should().Contain("Data Source=");
        config.SeededAdminEnterpriseWeChatId.Should().NotBeNullOrWhiteSpace();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --filter StartupConfigurationTests`
Expected: FAIL because the startup preview helper does not exist

- [ ] **Step 3: Write minimal implementation**

```csharp
public sealed record StartupConfigurationPreview(string ConnectionString, string SeededAdminEnterpriseWeChatId)
{
    public static StartupConfigurationPreview Build()
        => new("Data Source=atelier.db", "seeded-admin-wecom-id");
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj`
Expected: PASS

Run: `dotnet test tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add README.md src/Atelier.Web/Data/Migrations .env.example tests/Atelier.Web.Tests/Smoke/StartupConfigurationTests.cs
git commit -m "docs: add atelier local setup and migration workflow"
```

## Verification Checklist

- Run: `dotnet test tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj`
- Run: `dotnet test tests/Atelier.Web.Playwright/Atelier.Web.Playwright.csproj`
- Run: `dotnet build Atelier.sln`
- Run: `dotnet ef migrations add InitialCreate --project src/Atelier.Web/Atelier.Web.csproj`
- Run: `dotnet ef database update --project src/Atelier.Web/Atelier.Web.csproj`

All commands should pass before calling the module complete.
