# Atelier

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
