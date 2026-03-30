# Atelier

Atelier 是一个面向团队内部协作的计划与复盘平台，目标是把月计划、周报、月度分析、主管评审和计划修订串联成一个可追踪、可复盘、可审计的闭环流程，帮助团队建立更清晰的目标管理与执行反馈机制。

当前仓库实现的是基于 .NET 的 Plan and Review 模块 MVP。系统已经支持从月计划创建与激活、周报提报与重提、月度分析与评审，到修订生成与应用的完整工作流，并补充了截止时间规则、节假日处理、通知预览、审计日志、启动种子数据和本地迁移支持，适合用于内部试用、验收以及后续迭代开发。

## 当前已实现能力

- 月计划的创建、激活、调整与生命周期控制
- 基于激活月计划的周报提报、重提与截止时间处理
- 月度分析、主管评审与结果固化
- 计划修订建议生成与修订应用流程
- 站内通知预览与关键操作审计日志
- 本地 SQLite 启动、EF Core 迁移与启动初始化支持

## 技术栈

- ASP.NET Core Razor Pages
- Entity Framework Core
- SQLite（本地开发）
- 企业微信身份绑定与引导登录

## 仓库结构

- `src/Atelier.Web`：应用主代码
- `tests/Atelier.Web.Tests`：单元测试与集成测试
- `tests/Atelier.Web.Playwright`：端到端自动化测试
- `src/Atelier.Web/Data/Migrations`：EF Core 迁移文件

## 当前状态

当前仓库已经完成 Plan and Review 模块的 .NET MVP 开发，并通过自动化测试与端到端 happy path 验证，可用于本地试用和后续验收。

## Local NuGet restore

This repo checks in `.nupkgs/` so restore can run in this environment without relying on external package feeds.

`NuGet.Config` clears the default sources and points restore at `./.nupkgs`, so commands such as `dotnet restore tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --configfile NuGet.Config` resolve packages from the repo-local cache.

## Local setup

1. Copy `.env.example` values into your local environment.
2. Restore packages with `dotnet restore Atelier.sln --configfile NuGet.Config`.
3. Apply the SQLite schema with `dotnet ef database update --project src/Atelier.Web/Atelier.Web.csproj`.
4. Run the app with `dotnet run --project src/Atelier.Web/Atelier.Web.csproj`.

The app defaults to `Data Source=atelier.db` and seeds a bootstrap administrator from `ATELIER_BOOTSTRAP_ADMIN_ENTERPRISE_WECHAT_USER_ID` on startup.
Enterprise WeChat OAuth configuration comes from `ATELIER_ENTERPRISE_WECHAT_CLIENT_ID` and `ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET`.

## Migration workflow

Create a migration:

```bash
dotnet ef migrations add <Name> --project src/Atelier.Web/Atelier.Web.csproj --output-dir Data/Migrations
```

Apply migrations locally:

```bash
dotnet ef database update --project src/Atelier.Web/Atelier.Web.csproj
```

## Operator notes

- SQLite connection string comes from `ATELIER_SQLITE_CONNECTION_STRING` and defaults to `Data Source=atelier.db`.
- The bootstrap admin Enterprise WeChat identity comes from `ATELIER_BOOTSTRAP_ADMIN_ENTERPRISE_WECHAT_USER_ID`.
- Enterprise WeChat OAuth settings come from `ATELIER_ENTERPRISE_WECHAT_CLIENT_ID` and `ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET`.
- Startup runs schema initialization and seed data before serving requests.
