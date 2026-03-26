# Atelier

## Local NuGet restore

This repo checks in `.nupkgs/` so restore can run in this environment without relying on external package feeds.

`NuGet.Config` clears the default sources and points restore at `./.nupkgs`, so commands such as `dotnet restore tests/Atelier.Web.Tests/Atelier.Web.Tests.csproj --configfile NuGet.Config` resolve packages from the repo-local cache.
