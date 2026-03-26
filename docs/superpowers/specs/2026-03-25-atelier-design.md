# Atelier Design Spec

## Overview

Atelier is a modular work platform for small teams. The first module, `Plan and Review`, validates a closed-loop workflow for monthly planning, weekly reporting, variance analysis, monthly performance support, and month-end plan revision.

The product is intentionally platform-oriented, but the MVP only implements the first module. Future modules such as industry intelligence, knowledge capture, and collaboration support must plug into the platform as peers rather than being mixed into the planning workflow.

## Product Positioning

- Atelier is a work platform for helping small teams complete daily work more effectively.
- The MVP validates whether the `Plan and Review` module improves management quality and execution visibility.
- Monthly performance support is in scope, but only as a manager-assisted workflow. The system produces evidence and suggestions; team leads draft monthly assessments and administrators make the final monthly assessment.

## MVP Scope

### In Scope

- Platform shell with module entry points
- `Plan and Review` module
- Monthly plans with goals and key results
- Weekly report submission with structured fields and freeform notes
- KR-linked weekly progress updates
- Variance analysis based on KR progress and report patterns
- Monthly review records with manager-authored final conclusions
- Monthly plan revision suggestions and manager-selected application to the next month
- Roles: administrator, team lead, member
- Enterprise WeChat login
- Weekly deadline settings and reminder rules
- Audit logging for sensitive actions

### Out of Scope

- Industry intelligence implementation
- Personalized report templates
- Automatic performance scoring or payroll linkage
- Multi-organization / multi-tenant support
- DingTalk import or migration tooling
- Complex notification routing center
- Advanced analytics/reporting center

## Success Criteria

- The team can complete one full monthly cycle inside Atelier:
  - create a monthly plan
  - submit weekly reports
  - review variance analysis
  - complete monthly reviews
  - generate and apply next-month revision proposals
- The administrator can identify key monthly variance and decide next actions within 10 minutes.
- At least 3 members submit reports in the new system for 2 consecutive weeks.
- The administrator prefers continuing in Atelier over returning to DingTalk documents.

## Core Domain Model

### Platform Objects

- `Workspace`: the top-level organizational boundary. MVP supports exactly one workspace.
- `Team`: a sub-group inside the workspace. MVP supports multiple teams inside the one workspace.
- Each team has exactly one team lead in v1.
- `User`: a person authenticated through Enterprise WeChat, assigned to exactly one team and one role.

### Plan and Review Objects

- `MonthlyPlan`: a monthly planning container with month, status, creator, and timestamps.
- `MonthlyPlan` is workspace-scoped. There is one primary monthly plan per workspace per month.
- `Goal`: a monthly objective under a `MonthlyPlan`, with owner, priority, status, and description.
- `KeyResult`: a measurable result under a `Goal`, with target value, current value, owner, priority, due date, and status.
- `WeeklyReport`: a weekly submission from a member, containing structured report content plus additional notes.
- `KRUpdate`: a report item linked to one `KeyResult`, storing the updated absolute `currentValue` reported for that week plus optional execution notes and status context.
- `MonthlyReview`: a month-end performance support record containing system-generated evidence plus manager-authored final conclusions.
- `MonthlyPlanRevision`: a month-end set of revision suggestions and application choices for the next monthly plan draft.
- `AuditLog`: immutable records of sensitive changes such as deadline changes, review changes, and revision application.

## Information Architecture

### Platform Level

- Home / module launcher
- Shared identity, team, notification, and settings capabilities

### `Plan and Review` Module Pages

- `Monthly Plans`: create, view, activate, adjust, close, and archive monthly plans
- `Overview`: current month status, pending reports, overdue reminders, risk summary
- `Weekly Reports`: submit reports and review personal history
- `Analysis`: personal/team variance, stalled KR trends, blocker clusters, highlights
- `Monthly Reviews`: evidence summary plus manager final review entry
- `Revisions`: generated monthly revision suggestions and next-plan draft creation
- `Settings`: deadlines, reminder rules, team membership, and permission settings

## Roles and Permissions

### Administrator

- Manage platform-wide settings for the module
- Create and manage monthly plans
- View all planning, reporting, analysis, and review data
- Generate and apply monthly revision suggestions
- Review and verify monthly assessments globally

### Team Lead

- View planning, reporting, and review data for members in their team only
- Monitor overdue reports for their team
- Create and edit draft monthly reviews for their team members
- Move monthly reviews to manager-reviewed status for their team members

### Member

- View their own assigned goals and key results
- Submit weekly reports
- View their own history and final review results

### Sensitive Data Rules

- Members can only view their own final review and related feedback
- Team leads can only access review data for their own team
- Administrators can access the full data set
- Deadline changes, review changes, and revision application actions must be recorded in `AuditLog`
- Template customization is out of scope for v1; the weekly report uses one shared team-wide template

## Key Workflows and State Transitions

### 1. Monthly Plan Creation

1. Administrator creates a `MonthlyPlan`
2. Administrator adds one or more `Goal` records
3. Each `Goal` contains one or more `KeyResult` records
4. `MonthlyPlan` states:
   - `draft`
   - `active`
   - `closed`
   - `archived`

Editing rules:

- `draft` monthly plans may be fully edited by administrators
- `active` monthly plans may be adjusted only in limited ways by administrators
- Allowed edits while `active`: description, owner assignment, due date
- Structural changes to active plans, such as deleting goals, deleting KRs, or rewriting the plan without trace, are out of scope
- All edits to active monthly plans must be recorded in `AuditLog`
- `closed` and `archived` monthly plans are read-only
- A `MonthlyPlan` automatically moves from `active` to `closed` at the effective monthly close date
- Weekly reports for that month become read-only once the month is closed
- Monthly review finalization and monthly revision generation happen after the source month is closed

### 2. Weekly Reporting

Each member has at most one `WeeklyReport` per reporting week.

Reporting week definition:

- The reporting week uses timezone `Asia/Shanghai`
- A reporting week starts at Monday 00:00 and ends at Sunday 23:59:59
- The default submission deadline for that reporting week is Sunday 18:00 in `Asia/Shanghai`
- If that deadline falls on a China public holiday, it automatically moves to the next working day
- The holiday source for v1 is an internal holiday calendar maintained in the product and editable by administrators
- If an administrator manually changes a week's deadline, that changed deadline becomes the effective deadline date for lateness, reminders, and month attribution
- Weekly report uniqueness is `(user, reporting_week_start_date)`
- Cross-month reporting weeks belong to the month that contains the effective deadline date
- If a week's deadline requirement is disabled, month attribution still uses the planned effective deadline date for that reporting week, where planned means the latest configured deadline after any administrator change and holiday adjustment

Each `WeeklyReport` includes:

- weekly progress
- one or more `KRUpdate` items where possible
- zero or more structured unlinked work items for meaningful work that does not map to a KR
- risks / blockers
- next-week plan
- additional notes

`WeeklyReport` states:

- `draft`
- `submitted`

`late` is not a standalone workflow state. It is a system-derived flag on a submitted report.

Editing rules:

- A member may edit a `draft` report freely
- A submitted report may be edited and re-submitted until the monthly plan is closed
- If the report was submitted after deadline, or re-submitted after deadline, it remains marked `late`
- Reminder logic treats the latest submitted state as the source of truth, but `AuditLog` keeps submission and resubmission timestamps
- Each `KRUpdate` stores the updated absolute `currentValue` for its linked KR as of that reporting week
- Submitting a weekly report updates the canonical `KeyResult.currentValue` from the latest submitted `KRUpdate` values in that report
- A `KRUpdate` may also include optional notes explaining the change and whether the KR status should remain unchanged, move to `done`, or move to `dropped`
- Unlinked work items are stored as structured weekly report items with text, optional effort/importance notes, and no KR reference
- Focus-drift analysis counts both KR-linked updates and structured unlinked work items when computing how much work stayed outside high-priority KR execution
- Each blocker entry may optionally link to a specific `KRUpdate`
- If a blocker is linked to a `KRUpdate`, it is treated as linked to that KR for analysis
- If a blocker is not linked to a `KRUpdate`, it remains a report-level blocker and is excluded from KR-specific repeated-blocker detection

### 3. Variance Analysis

The system analyzes `KeyResult` progress and linked `KRUpdate` activity to produce:

- progress variance
- continuous lack of progress
- repeated blockers
- goal overload or focus drift

Analysis is evidence only. It does not create a final performance decision.

### 4. Monthly Review

The system compiles:

- goal and KR completion summaries
- weekly report timeliness and completeness
- risk exposure and handling patterns
- highlights and issues

Administrators finalize the record in `MonthlyReview` after team leads prepare the draft.

Creation rules:

- Each user has exactly one `MonthlyReview` record per month
- That record is first created as `draft`
- Team leads may create the initial draft review for members in their own team
- If no draft exists yet, an administrator may also create it
- Later edits update the same monthly review record rather than creating parallel drafts

Workflow ownership:

- Team leads may create and edit `draft` reviews for members in their own team
- Team leads may move a review to `manager_reviewed`
- Team leads may propose the draft conclusion and draft rating for members in their own team
- Administrators are the final approvers for v1 monthly reviews and own the final conclusion and final rating
- Administrators may create, edit, review, and finalize any review
- Only administrators may move a review to `finalized`
- Finalized monthly reviews are locked for normal editing
- If an administrator must correct a finalized review, the correction must create an audit-logged amendment rather than silently editing history

`MonthlyReview` states:

- `draft`
- `manager_reviewed`
- `finalized`

### 5. Monthly Plan Revision

The system generates `MonthlyPlanRevision` suggestions such as:

- keep
- defer
- downgrade priority
- remove
- add compensating KR

Administrators choose which suggestions to apply. Applying them creates the next `MonthlyPlan` draft rather than modifying the current active month in place.

Revision application rules:

- There is only one next-month `MonthlyPlan` draft per month
- If no next-month draft exists, applying revisions creates it
- If a next-month draft already exists, selected revisions merge into the existing draft instead of creating another one
- Applying revisions multiple times is allowed; each application must be audit-logged with what changed
- Already-applied revision items must be shown as applied to prevent silent duplication
- Each revision item carries a stable source identity made from `(source month, source goal or KR id, suggestion type)`
- Re-applying the same revision item must not create a duplicate target item if that source identity is already linked to a draft item
- If the existing draft item has been manually edited after the earlier application, the new revision attempt is marked `conflict_skipped` rather than overwriting user edits
- Conflict handling in v1 is conservative: the system skips conflicting items, shows them to the administrator, and requires manual resolution
- Each revision application result must be recorded as one of `applied`, `skipped_duplicate`, or `conflict_skipped`
- Revision suggestions are generated manually on demand by an administrator in v1
- Suggestion semantics in v1:
  - `keep`: copy the source Goal or KR into the next-month draft with the same priority unless the administrator changes it manually
  - `defer`: copy the source Goal or KR into the next-month draft and mark it as carried over from the prior month
  - `remove`: do not copy the source Goal or KR into the next-month draft
  - `remove`: if the source Goal or KR had already been copied into the next-month draft by an earlier revision application and has not been manually edited, mark it for removal from that draft; if it was manually edited, skip with `conflict_skipped`
  - `add compensating KR`: create a new KR in the next-month draft under the mapped Goal from the source item; if that Goal is not yet present in the draft, the system first copies the parent Goal into the draft and then adds the compensating KR under it
  - `downgrade priority`: copy the source Goal or KR into the next-month draft with a lower priority than the current month

## Analysis and Review Rules

### KR-Centered Analysis

- `KeyResult` is the primary analysis unit.
- `Goal` is only used as the aggregate health view.

Each KR should expose:

- progress variance
- continuity of execution
- risk status

V1 analysis uses a bounded semi-smart model:

- deterministic rules produce the primary flags
- v1 does not require a live AI provider integration
- v1 must be fully functional with deterministic keyword normalization and template-based summaries only
- If the implementation leaves an AI hook for future use, it must be provider-agnostic and optional at runtime
- Any future AI integration may be used only for improved blocker clustering and evidence summaries for managers
- AI does not set final ratings or override rule-derived facts

Priority rules:

- `Goal` has a required priority
- `KeyResult` also has a required priority
- The v1 priority enum is fixed as `high`, `medium`, `low`
- Priority ordering is `high` > `medium` > `low`
- A new KR inherits the parent Goal priority by default, but administrators may override it when finer control is needed

KR metric rules:

- v1 supports numeric-progress KRs only
- Each KR stores `currentValue` and `targetValue` as numbers
- `targetValue` must be greater than zero
- KR completion percentage is calculated as `(currentValue / targetValue) * 100`, capped to the range `0` to `100`
- Variance analysis uses this normalized completion percentage as the actual progress value

Status rules:

- `Goal` and `KeyResult` both use the same v1 status set:
  - `draft`
  - `active`
  - `done`
  - `dropped`
- When a `MonthlyPlan` moves from `draft` to `active`, all draft Goals and KeyResults in that plan automatically move to `active`
- Administrators may manually mark Goals and KeyResults as `done` or `dropped`
- Team leads do not change Goal or KeyResult status directly in v1; they influence status through weekly reporting and monthly review inputs
- `active` means the item is currently in execution and should be included in operational analysis, dashboard counts, and reminder logic
- `done` means the item is completed for the current monthly cycle
- `dropped` means the item is intentionally abandoned and excluded from active execution analysis

Rule definitions:

- `progress variance`
  - Convert each KR to a normalized completion percentage from 0 to 100
  - Compute expected progress from elapsed calendar days in the active month
  - Flag `at_risk` when actual progress trails expected progress by 20 percentage points or more
  - Flag `critical` when actual progress trails expected progress by 40 percentage points or more
- `continuous lack of progress`
  - Trigger when a KR has no `KRUpdate` with either progress change or status change for 2 consecutive reporting weeks
- `repeated blockers`
  - Extract blocker text from weekly reports linked to the same KR or Goal in the same month
  - Use deterministic text normalization and rule-based grouping to group semantically similar blockers in v1
  - Flag when the same blocker cluster appears in 2 or more weekly reports in the same month
- `goal overload`
  - Flag when one owner has more than 3 active Goals or more than 5 active KRs in the same month and at least one high-priority KR is already `at_risk`
- `focus drift`
  - Flag when 50% or more of a member's weekly report updates are either unlinked to any KR or linked only to lower-priority Goals for 2 consecutive weeks while a high-priority KR shows no progress

### Weekly Report Quality

The MVP uses lightweight rules instead of heavy NLP scoring:

- whether the report links to KR updates
- whether progress is described
- whether risks/blockers are present
- whether next-week planning is present

This measures report usefulness, not writing quality.

### Monthly Performance Support

The MVP uses a manager-assisted model:

- the system provides evidence and suggestions
- managers produce final conclusions and ratings

Recommended rating set for v1:

- `Exceeds Expectations`
- `Meets Expectations`
- `Needs Improvement`

Rating is required in v1 and must use this fixed enum set.

### Revision Rules

Revision suggestions must be traceable to evidence. Black-box recommendations are out of scope.

## Reminders, Notifications, and Exception Handling

### Deadlines and Reminders

- Default weekly report deadline: Sunday 18:00
- Administrator can modify a given week’s deadline
- Administrator can disable the deadline requirement for a given week
- The effective monthly close date is the last natural day of the calendar month
- MVP delivery channel is in-app notification only
- Enterprise WeChat is used for authentication in v1, not for reminder delivery
- Reminder timing is fixed in v1, not admin-configurable
- In v1, `reminder rules` means administrators can change or disable the weekly deadline; they cannot change reminder timing offsets
- Reminder behavior:
  - remind the member 24 hours before deadline
  - if overdue, remind the team lead after 24 hours

### Notification Events

- report due soon
- report overdue
- monthly review pending
- monthly revision suggestions ready
- deadline changed
- deadline disabled for the week

Notification matrix for v1:

- `report due soon`
  - recipient: the member who has not yet submitted the weekly report
  - trigger: 24 hours before the effective weekly deadline
  - timing: one-time in-app event
- `report overdue`
  - recipient: the member's team lead
  - trigger: the weekly report is still missing 24 hours after the effective weekly deadline
  - timing: one-time in-app event
- `monthly review pending`
  - recipient: the responsible team lead
  - trigger: a member's monthly review remains in `draft` at 18:00 on the second working day after the effective monthly close date
  - timing: one-time in-app event per pending review cycle
- `monthly revision suggestions ready`
  - recipient: administrators
  - trigger: an administrator manually generates monthly revision suggestions
  - timing: one-time in-app event per generation action
- `deadline changed`
  - recipient: affected members and their team leads
  - trigger: an administrator changes the weekly deadline
  - timing: one-time in-app event
- `deadline disabled for the week`
  - recipient: affected members and their team leads
  - trigger: an administrator disables the weekly deadline for that reporting week
  - timing: one-time in-app event

### Exception Rules

- A member may submit a report without a KR link, but it must be marked as unlinked
- Late submission is allowed and must remain visible as late
- Mid-week deadline changes must take effect from the latest rule and be audit-logged
- Closed monthly plans cannot be silently rewritten
- Enterprise WeChat login failures should transition to a recoverable authorization/binding state

### Missing Data vs Poor Execution

The analysis view must distinguish between:

- no real progress
- missing weekly report
- submitted report without KR linkage

This prevents management from confusing lack of data with poor performance.

## Technical Architecture and Expansion Strategy

### MVP Architecture

- Start with a single ASP.NET Core web app using Razor Pages
- Keep clear internal module boundaries
- Separate platform concerns from module-specific business rules
- Do not overbuild generic module infrastructure beyond what is needed to support `Plan and Review` plus one future module slot

### Technology Stack

- Web framework: `ASP.NET Core Razor Pages`
- ORM: `EF Core`
- Local database: `SQLite`
- Cloud database target: `PostgreSQL`
- Authentication: `Enterprise WeChat OAuth` only in v1
- UI style: server-rendered management console, not SPA-first
- Vue is not part of the v1 stack
- If partial dynamic refresh is needed in v1, it should be solved with server-rendered page patterns first and lightweight progressive enhancement second, not by introducing a separate frontend application

### Why Razor Pages

- The MVP is dominated by forms, tables, filters, review flows, and admin settings
- Razor Pages keeps page markup and handler logic close together, which is well-suited to a traditional back-office system
- It reduces moving parts compared with a split frontend/API architecture and keeps v1 easier to build and revise

### Layers

- Platform layer: authentication, teams, roles, navigation, notifications
- Module layer: `Plan and Review`, future `Industry Intelligence`
- Domain layer: planning, reporting, review, revision business rules
- Data layer: relational records, audit logs, future intelligence storage
- Integration layer: Enterprise WeChat login, future external intelligence sources

### Data and Deployment Strategy

- Use SQLite for local-first development so the MVP can run on one machine with minimal setup
- Keep the EF Core model portable so the production deployment can move to PostgreSQL without changing the domain model
- Treat authentication, database configuration, and notification infrastructure as deploy-time concerns rather than hard-coded local assumptions

### Why Single App First

- The team is small
- The product is still being shaped
- Premature service splitting would slow iteration and add unnecessary complexity

### Authentication Constraint

- v1 supports Enterprise WeChat login only
- Consumer WeChat login is out of scope
- If a valid Enterprise WeChat identity cannot be mapped to an internal user, the product must place the user in a recoverable binding state instead of failing silently
- In the recoverable binding state, the user can only see a waiting-for-binding screen and cannot access business pages
- Administrators are responsible for creating or binding the internal user record to the Enterprise WeChat identity
- After the administrator completes binding, the user can sign in again and enter the platform with the assigned role and team scope
- The first administrator is provisioned from seeded configuration during deployment or local setup and is mapped to a known Enterprise WeChat identity before first use

### Expansion Constraint

Future modules such as `Industry Intelligence` must integrate at the platform/module boundary. They must not be embedded into report objects or the planning domain model.

## Future Module Direction

Planned future module families include:

- Industry intelligence
- Knowledge capture
- Collaboration support

These are not part of the MVP but the platform should leave room for them in navigation, permissions, and module registration.

## Product Boundary Summary

Atelier v1 is not a general-purpose all-in-one work platform. It is a platform-shaped product whose first implemented module validates whether a plan-execution-review-revision loop works better than the current DingTalk document workflow for a small team.
