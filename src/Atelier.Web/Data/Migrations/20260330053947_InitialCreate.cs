using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atelier.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendarEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendarEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HolidayCalendarEntries_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Blockers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeeklyReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    KrUpdateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Impact = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blockers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonthlyPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    TargetValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyResults_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KrUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeeklyReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    KeyResultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExecutionNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KrUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KrUpdates_KeyResults_KeyResultId",
                        column: x => x.KeyResultId,
                        principalTable: "KeyResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPlanRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceMonthlyPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceGoalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceKeyResultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceIdentity = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SuggestionType = table.Column<int>(type: "INTEGER", nullable: false),
                    ApplicationResult = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPlanRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPlanRevisions_Goals_SourceGoalId",
                        column: x => x.SourceGoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyPlanRevisions_KeyResults_SourceKeyResultId",
                        column: x => x.SourceKeyResultId,
                        principalTable: "KeyResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanMonth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPlans_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonthlyPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DraftedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    DraftConclusion = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    FinalConclusion = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    DraftRating = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FinalRating = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyReviews_MonthlyPlans_MonthlyPlanId",
                        column: x => x.MonthlyPlanId,
                        principalTable: "MonthlyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TeamLeadUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnterpriseWeChatUserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonthlyPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportingWeekStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveDeadlineDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLate = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeeklyProgress = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    NextWeekPlan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AdditionalNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ReadOnlyAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReports_MonthlyPlans_MonthlyPlanId",
                        column: x => x.MonthlyPlanId,
                        principalTable: "MonthlyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnlinkedWorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeeklyReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlinkedWorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlinkedWorkItems_WeeklyReports_WeeklyReportId",
                        column: x => x.WeeklyReportId,
                        principalTable: "WeeklyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_WorkspaceId",
                table: "AuditLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Blockers_KrUpdateId",
                table: "Blockers",
                column: "KrUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_Blockers_WeeklyReportId",
                table: "Blockers",
                column: "WeeklyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_MonthlyPlanId",
                table: "Goals",
                column: "MonthlyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_OwnerUserId",
                table: "Goals",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendarEntries_WorkspaceId_Date",
                table: "HolidayCalendarEntries",
                columns: new[] { "WorkspaceId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_GoalId",
                table: "KeyResults",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_OwnerUserId",
                table: "KeyResults",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KrUpdates_KeyResultId",
                table: "KrUpdates",
                column: "KeyResultId");

            migrationBuilder.CreateIndex(
                name: "IX_KrUpdates_WeeklyReportId",
                table: "KrUpdates",
                column: "WeeklyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlanRevisions_CreatedByUserId",
                table: "MonthlyPlanRevisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlanRevisions_SourceGoalId",
                table: "MonthlyPlanRevisions",
                column: "SourceGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlanRevisions_SourceIdentity",
                table: "MonthlyPlanRevisions",
                column: "SourceIdentity");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlanRevisions_SourceKeyResultId",
                table: "MonthlyPlanRevisions",
                column: "SourceKeyResultId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlanRevisions_SourceMonthlyPlanId",
                table: "MonthlyPlanRevisions",
                column: "SourceMonthlyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlans_CreatedByUserId",
                table: "MonthlyPlans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlans_WorkspaceId_PlanMonth_IsPrimary",
                table: "MonthlyPlans",
                columns: new[] { "WorkspaceId", "PlanMonth", "IsPrimary" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReviews_DraftedByUserId",
                table: "MonthlyReviews",
                column: "DraftedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReviews_FinalizedByUserId",
                table: "MonthlyReviews",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReviews_MonthlyPlanId_UserId",
                table: "MonthlyReviews",
                columns: new[] { "MonthlyPlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReviews_UserId",
                table: "MonthlyReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamLeadUserId",
                table: "Teams",
                column: "TeamLeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_WorkspaceId_Name",
                table: "Teams",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnlinkedWorkItems_WeeklyReportId",
                table: "UnlinkedWorkItems",
                column: "WeeklyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EnterpriseWeChatUserId",
                table: "Users",
                column: "EnterpriseWeChatUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TeamId",
                table: "Users",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkspaceId",
                table: "Users",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_MonthlyPlanId",
                table: "WeeklyReports",
                column: "MonthlyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_UserId_ReportingWeekStartDate",
                table: "WeeklyReports",
                columns: new[] { "UserId", "ReportingWeekStartDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blockers_KrUpdates_KrUpdateId",
                table: "Blockers",
                column: "KrUpdateId",
                principalTable: "KrUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blockers_WeeklyReports_WeeklyReportId",
                table: "Blockers",
                column: "WeeklyReportId",
                principalTable: "WeeklyReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_MonthlyPlans_MonthlyPlanId",
                table: "Goals",
                column: "MonthlyPlanId",
                principalTable: "MonthlyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Users_OwnerUserId",
                table: "Goals",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyResults_Users_OwnerUserId",
                table: "KeyResults",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KrUpdates_WeeklyReports_WeeklyReportId",
                table: "KrUpdates",
                column: "WeeklyReportId",
                principalTable: "WeeklyReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyPlanRevisions_MonthlyPlans_SourceMonthlyPlanId",
                table: "MonthlyPlanRevisions",
                column: "SourceMonthlyPlanId",
                principalTable: "MonthlyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyPlanRevisions_Users_CreatedByUserId",
                table: "MonthlyPlanRevisions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyPlans_Users_CreatedByUserId",
                table: "MonthlyPlans",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyReviews_Users_DraftedByUserId",
                table: "MonthlyReviews",
                column: "DraftedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyReviews_Users_FinalizedByUserId",
                table: "MonthlyReviews",
                column: "FinalizedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyReviews_Users_UserId",
                table: "MonthlyReviews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_TeamLeadUserId",
                table: "Teams",
                column: "TeamLeadUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_TeamLeadUserId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Blockers");

            migrationBuilder.DropTable(
                name: "HolidayCalendarEntries");

            migrationBuilder.DropTable(
                name: "MonthlyPlanRevisions");

            migrationBuilder.DropTable(
                name: "MonthlyReviews");

            migrationBuilder.DropTable(
                name: "UnlinkedWorkItems");

            migrationBuilder.DropTable(
                name: "KrUpdates");

            migrationBuilder.DropTable(
                name: "KeyResults");

            migrationBuilder.DropTable(
                name: "WeeklyReports");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "MonthlyPlans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Workspaces");
        }
    }
}
