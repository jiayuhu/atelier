using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

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

    [Fact]
    public void AtelierDbContext_MapsCorePlatformAndPlanReviewAggregates()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AtelierDbContext(options);

        var model = context.Model;
        var weeklyReportEntity = model.FindEntityType(typeof(WeeklyReport));
        var monthlyPlanEntity = model.FindEntityType(typeof(MonthlyPlan));
        var teamEntity = model.FindEntityType(typeof(Team));

        monthlyPlanEntity.Should().NotBeNull();
        model.FindEntityType(typeof(Goal)).Should().NotBeNull();
        model.FindEntityType(typeof(KeyResult)).Should().NotBeNull();
        weeklyReportEntity.Should().NotBeNull();
        model.FindEntityType(typeof(KrUpdate)).Should().NotBeNull();
        model.FindEntityType(typeof(UnlinkedWorkItem)).Should().NotBeNull();
        model.FindEntityType(typeof(Blocker)).Should().NotBeNull();
        model.FindEntityType(typeof(MonthlyReview)).Should().NotBeNull();
        model.FindEntityType(typeof(MonthlyPlanRevision)).Should().NotBeNull();
        teamEntity.Should().NotBeNull();

        weeklyReportEntity!
            .GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(WeeklyReport.UserId), nameof(WeeklyReport.ReportingWeekStartDate) }));

        monthlyPlanEntity!
            .GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(MonthlyPlan.WorkspaceId), nameof(MonthlyPlan.PlanMonth), nameof(MonthlyPlan.IsPrimary) }));

        teamEntity!
            .FindProperty(nameof(Team.TeamLeadUserId))!
            .IsNullable
            .Should()
            .BeTrue();
    }
}
