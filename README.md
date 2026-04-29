# Dotnetstore.MinimalApi

[![CI](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/ci.yml)
[![CD](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/cd.yml/badge.svg?branch=main)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/cd.yml)
[![PR Title](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/pr-title.yml/badge.svg)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/pr-title.yml)
[![Auto Label](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/auto-label.yml/badge.svg)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/auto-label.yml)
[![Stale Cleanup](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/stale.yml/badge.svg)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/stale.yml)
[![Container Publish](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/container-publish.yml/badge.svg?branch=main)](https://github.com/Dotnetstore/Dotnetstore.MinimalApi/actions/workflows/container-publish.yml)
[![Azure Deployment Ready](https://img.shields.io/badge/Azure-App_Service-0078D4?logo=microsoftazure&logoColor=white)](https://learn.microsoft.com/azure/app-service/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)
[![Container Registry](https://img.shields.io/badge/GHCR-ready-2496ED?logo=docker&logoColor=white)](https://github.com/users/Dotnetstore/packages/container/package/dotnetstore-minimalapi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Reusable .NET 10 Minimal API template for teams that want a production-minded starting point instead of a blank project. It comes with API versioning, API-key protection, CORS, rate limiting, OpenTelemetry-ready defaults, OpenAPI + Scalar API docs for development, Aspire orchestration, Docker packaging, automated tests, and GitHub workflows for CI/CD, releases, coverage, and container publishing.

## Why use this template?

Use this repository when you want to start a new API with the platform pieces already in place.

Included out of the box:

- Minimal API app with a sample `/test` endpoint
- API version header support
- API key endpoint protection
- CORS and rate limiting configuration
- OpenTelemetry tracing and metrics hooks
- OpenAPI JSON plus Scalar UI for local API exploration
- Aspire `AppHost` for local orchestration
- Dockerfile and `docker-compose.yml` for containerized runs
- automated tests and coverage reporting
- GitHub Actions workflows for CI, CD, releases, and container publishing

The sample `/test` endpoint is intentionally simple so you can use it as a smoke test while replacing template code with your own domain endpoints.

## Quick start

### 1. Create your API from this template

Typical adoption flow:

1. create a new repository from this template or clone it
2. rename the solution, projects, namespaces, container image names, and repository metadata
3. replace the sample `/test` endpoint with your own endpoints
4. change the sample `ApiKey` value before sharing the API with anyone else
5. keep or trim the GitHub workflows depending on how much automation you want

### First 10 minutes after cloning

Use this as a quick onboarding checklist before you start building real endpoints:

1. rename the solution, projects, namespaces, repository, and container image names
2. change the sample `ApiKey:Value` in configuration or override it from environment variables
3. review `WebApi:Cors:AllowedOrigins` so it matches your frontend or calling clients
4. run the sample `/test` endpoint once to confirm the template works in your environment
5. decide whether you want to keep the included GitHub Actions, release automation, and Azure deployment workflow

Tiny rename examples:

```powershell
git mv .\src\Dotnetstore.MinimalApi.Api.WebApi .\src\Contoso.Orders.Api
git mv .\tests\Dotnetstore.MinimalApi.Api.WebApi.Tests .\tests\Contoso.Orders.Api.Tests
git mv .\src\Dotnetstore.MinimalApi.Shared.AppHost .\src\Contoso.Orders.AppHost
```

After renaming folders and project files, use your IDE rename/refactor tools to update namespaces, project references, and solution contents together.

### 2. Restore, build, and run tests

```powershell
dotnet restore .\Dotnetstore.MinimalApi.slnx
dotnet build .\Dotnetstore.MinimalApi.slnx --configuration Release --no-restore
dotnet run --project .\tests\Dotnetstore.MinimalApi.Api.WebApi.Tests\Dotnetstore.MinimalApi.Api.WebApi.Tests.csproj --configuration Release --no-build
```

### 3. Run the API locally

HTTP only:

```powershell
dotnet run --project .\src\Dotnetstore.MinimalApi.Api.WebApi\Dotnetstore.MinimalApi.Api.WebApi.csproj --launch-profile http
```

HTTP + HTTPS:

```powershell
dotnet run --project .\src\Dotnetstore.MinimalApi.Api.WebApi\Dotnetstore.MinimalApi.Api.WebApi.csproj --launch-profile https
```

Launch profile URLs:

- `http://localhost:5126`
- `https://localhost:7201`

Smoke test the sample endpoint:

```powershell
curl.exe -i http://localhost:5126/test -H "api-version: 1.0" -H "X-API-KEY: 5CC5F891B1A44E45BCFAB72B598515CA"
curl.exe -k -i https://localhost:7201/test -H "api-version: 1.0" -H "X-API-KEY: 5CC5F891B1A44E45BCFAB72B598515CA"
```

Development-only API docs:

- Scalar UI: `https://localhost:7201/docs/`
- OpenAPI JSON: `https://localhost:7201/openapi/v1.json`

The project maps both endpoints only when running in `Development`. The generated OpenAPI document includes the configured `ApiKey` security scheme, and Scalar is preconfigured to send the current API key header name for trying secured endpoints locally.

Replace the sample `/test` endpoint:

1. update or rename `src/Dotnetstore.MinimalApi.Api.WebApi/Endpoints/TestEndpoints.cs`
2. keep the `ITestEndpoints.MapEndpoints(WebApplication app)` shape, or replace that abstraction everywhere consistently
3. swap `app.MapGet("/test", ...)` for your own route groups and handlers
4. keep or remove `.AddEndpointFilter<ApiKeyFilter>()` depending on whether you want API key protection on the new endpoint
5. keep `Program.cs` resolving `ITestEndpoints` unless you move endpoint registration elsewhere

Today the sample endpoint is wired like this:

- `Program.cs` resolves `ITestEndpoints` and calls `MapEndpoints(app)`
- `Extensions/WebApplicationExtensions.cs` registers `ITestEndpoints` in DI
- `Endpoints/TestEndpoints.cs` defines the sample `/test` route

## Solution structure

- `src/Dotnetstore.MinimalApi.Api.WebApi` - the deployable Minimal API application
- `src/Dotnetstore.MinimalApi.Shared.AppHost` - Aspire orchestration for local development
- `src/Dotnetstore.MinimalApi.Shared.ServiceDefaults` - shared service defaults for health, resilience, logs, traces, and metrics
- `tests/Dotnetstore.MinimalApi.Api.WebApi.Tests` - startup, middleware, configuration, and endpoint tests
- `.github/workflows` - CI/CD, release, validation, and container workflows
- `docs/branch-protection.md` - suggested branch protection settings

## Configuration and settings

The template uses normal ASP.NET Core configuration layering, so you can customize behavior by environment without changing code.

### Which settings file is used?

| Scenario | Environment | Files applied |
| --- | --- | --- |
| Local development | `Development` | `appsettings.json` + `appsettings.Development.json` |
| Docker / Docker Compose | `Docker` | `appsettings.json` + `appsettings.Docker.json` |
| Other environments | value of `ASPNETCORE_ENVIRONMENT` | `appsettings.json` + `appsettings.{Environment}.json` if present |

### Common settings scenarios

| Scenario | Environment | Main place to change settings | Common values to review |
| --- | --- | --- | --- |
| Local frontend development | `Development` | `appsettings.Development.json` | `WebApi:Cors:AllowedOrigins`, `WebApi:HttpsRedirection:HttpsPort`, `ApiKey` |
| Docker Desktop run | `Docker` | `appsettings.Docker.json` plus compose environment variables | `WebApi:Hsts:Enabled=false`, `WebApi:HttpsRedirection:Enabled=false`, `ApiKey`, optional `OTEL_EXPORTER_OTLP_*` |
| Hosted deployment | environment-specific file and environment variables / secret store | environment variables for secrets, deployment-time overrides for `ApiKey`, `WebApi:OpenTelemetry:ServiceName`, CORS origins, rate limits |

Current defaults in this template:

- local launch profiles set `ASPNETCORE_ENVIRONMENT=Development`
- `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Docker`
- Aspire runs the `webApi` resource and wires telemetry/exporter settings for local orchestration

### What belongs in each file?

- `src/Dotnetstore.MinimalApi.Api.WebApi/appsettings.json` - shared defaults for every environment
- `src/Dotnetstore.MinimalApi.Api.WebApi/appsettings.Development.json` - local developer overrides
- `src/Dotnetstore.MinimalApi.Api.WebApi/appsettings.Docker.json` - container-specific overrides for Docker Desktop runs

### Main configuration sections

| Section | Purpose |
| --- | --- |
| `WebApi:Cors` | allowed origins and methods |
| `WebApi:Hsts` | HSTS behavior |
| `WebApi:HttpsRedirection` | redirect behavior and HTTPS port |
| `WebApi:OpenTelemetry` | service identity plus tracing/metrics toggles |
| `WebApi:RateLimiting` | global and short-window rate limits |
| `ApiKey` | required header name and expected API key value |
| `Logging`, `Kestrel`, `AllowedHosts` | standard ASP.NET Core host settings |

> [!IMPORTANT]
> The `ApiKey` value committed in `appsettings.json` is a sample template value. Change it before using this repository for a real API.

### Override settings with environment variables

Use double underscores (`__`) for nested values.

```powershell
$env:ApiKey__Value = "change-me-before-sharing"
$env:WebApi__Cors__AllowedOrigins__0 = "http://localhost:3000"
$env:WebApi__RateLimiting__GlobalPermitLimit = "100"
dotnet run --project .\src\Dotnetstore.MinimalApi.Api.WebApi\Dotnetstore.MinimalApi.Api.WebApi.csproj --launch-profile https
```

This is the preferred way to provide secrets or environment-specific values in CI, containers, and hosted deployments.

### Common customization examples

Change local development settings only:

```json
{
  "WebApi": {
    "Cors": {
      "AllowedOrigins": [
        "http://localhost:3000"
      ],
      "AllowedMethods": [
        "GET",
        "POST",
        "PUT"
      ]
    },
    "HttpsRedirection": {
      "RedirectStatusCode": 307,
      "HttpsPort": 7201
    }
  }
}
```

Change Docker behavior only:

```json
{
  "WebApi": {
    "Hsts": {
      "Enabled": false
    },
    "HttpsRedirection": {
      "Enabled": false
    },
    "RateLimiting": {
      "GlobalPermitLimit": 100
    }
  }
}
```

Override values from a shell or pipeline:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Docker"
$env:ApiKey__Value = "super-secret-value"
$env:WebApi__OpenTelemetry__ServiceName = "orders-api"
$env:WebApi__RateLimiting__ShortPermitLimit = "20"
dotnet run --project .\src\Dotnetstore.MinimalApi.Api.WebApi\Dotnetstore.MinimalApi.Api.WebApi.csproj --no-launch-profile
```

### Validation behavior

The application validates `WebApi` options during startup and fails fast when configuration is malformed.

Examples of validated values:

- `Cors.AllowedOrigins` and `Cors.AllowedMethods` must contain non-empty values
- `Hsts.MaxAgeDays` must be greater than `0` when HSTS is enabled
- HTTPS redirect status codes must be in the `300`-`399` range when redirection is enabled
- HTTPS ports must be between `1` and `65535`
- `OpenTelemetry.ServiceName` cannot be blank
- excluded trace paths cannot contain blank values
- rate-limit permit and window values must be positive
- queue limits cannot be negative
- rejection and fallback messages cannot be blank

Docker disables HSTS and HTTPS redirection by default because the compose setup exposes the app on plain HTTP at `http://localhost:8080`.

## Run modes

### Run with Aspire

Use Aspire when you want the best local orchestration experience and built-in observability.

```powershell
dotnet run --project .\src\Dotnetstore.MinimalApi.Shared.AppHost\Dotnetstore.MinimalApi.Shared.AppHost.csproj
```

This starts the `webApi` resource through the Aspire `AppHost`, and local telemetry/exporter settings are supplied by Aspire.

When running in `Development`, you can also open the API reference UI at `/docs/` on the Web API service and inspect the generated OpenAPI document at `/openapi/v1.json`.

### Run with Docker Compose

Use Docker Compose when you want to run the deployable container from Docker Desktop.

What `docker-compose.yml` does for you:

- builds the image from the root `Dockerfile`
- sets `ASPNETCORE_ENVIRONMENT=Docker`
- exposes the API on `http://localhost:8080`
- optionally accepts OTLP exporter settings from environment variables

```powershell
docker compose up --build
```

Smoke test:

```powershell
curl.exe -i http://localhost:8080/test -H "api-version: 1.0" -H "X-API-KEY: 5CC5F891B1A44E45BCFAB72B598515CA"
```

Useful follow-up commands:

```powershell
docker compose logs -f webapi
docker compose down
```

### Run the published image directly

```powershell
docker build -t dotnetstore-minimalapi:local .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Docker dotnetstore-minimalapi:local
```

### Run Docker with optional observability

The compose file also includes an optional `observability` profile that starts the Aspire dashboard container and points the API at it through `.env.observability`.

```powershell
docker compose --profile observability --env-file .\.env.observability up --build
```

Endpoints:

- Web API: `http://localhost:8080`
- Aspire dashboard: `http://localhost:18888`

The `.env.observability` file provides:

- `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`
- `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`

That means telemetry export stays off for the normal Docker experience and turns on only when you opt into the observability profile.

## Template customization checklist

When turning this repository into your own API, review these items first:

### Rename these first

These identifiers are the most obvious template leftovers and are usually worth changing immediately:

- solution and project names such as `Dotnetstore.MinimalApi`
- C# namespaces under `src` and `tests`
- repository name, description, and remote URL
- container image names in `Dockerfile`, `docker-compose.yml`, and workflow configuration
- GitHub Container Registry references such as `ghcr.io/dotnetstore/dotnetstore-minimalapi`
- badge links, documentation references, and any organization-specific metadata

### Then review the rest of the template defaults

- replace the sample `/test` endpoint with your own route groups/endpoints
- update `ApiKey` settings or replace API-key auth with your preferred auth approach
- update CORS origins, rate limits, and OpenTelemetry service name
- review GitHub workflows, labels, templates, and release automation
- review Docker image naming and publish targets
- update repository URLs, badges, and package/container references

## Repository automation included

This template includes GitHub automation you can keep as-is or trim down.

### CI and delivery

- `CI` restores, builds, tests, and publishes coverage artifacts
- `CD` publishes the Web API and can deploy to Azure App Service
- `Container Publish` builds and pushes an image to GitHub Container Registry

Published container image name:

```text
ghcr.io/dotnetstore/dotnetstore-minimalapi
```

### Release and repository management

- `Release Please` manages semantic versioning and changelog generation
- `Release Drafter` keeps draft release notes updated
- `pr-title.yml` and `commit-message.yml` enforce lightweight Conventional Commits rules
- `auto-label.yml` applies labels automatically
- `stale.yml` handles stale issues and pull requests
- `.github/CODEOWNERS` supports review ownership

Examples of accepted commit or PR titles:

- `feat: add order endpoint`
- `fix(api): correct CORS preflight handling`
- `test(webapi): add startup coverage`

### Dependabot and Discussions

- `.github/dependabot.yml` checks NuGet and GitHub Actions dependencies weekly
- `.github/DISCUSSION_TEMPLATE` contains starter templates for ideas, Q&A, and show-and-tell posts

## Azure deployment

The `CD` workflow contains an optional Azure App Service deployment job.

It is skipped unless these repository secrets exist:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_WEBAPP_NAME`

Behavior:

- pushes to `main` deploy to a staging slot by default
- `AZURE_STAGING_SLOT_NAME` can override the staging slot name
- tags matching `v*` deploy to production
- manual dispatch can target `staging` or `production`

Example App Service configuration values:

Use App Service Configuration or deployment-time environment variables for values like these:

```text
ASPNETCORE_ENVIRONMENT=Production
ApiKey__Value=replace-with-a-secret-value
WebApi__Cors__AllowedOrigins__0=https://app.contoso.com
WebApi__OpenTelemetry__ServiceName=contoso-orders-api
```

This keeps secrets out of source control while still letting you override the shared defaults from `appsettings.json`.

## Branch protection and releases

Recommended branch protection settings are documented in `docs/branch-protection.md`.

To create a versioned release artifact manually:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

That tag triggers release packaging and container publishing. `Release Please` can also create tags and releases automatically when its release PR is merged.

## Generate a local coverage report

```powershell
dotnet tool install dotnet-coverage --tool-path .\.tools
dotnet tool install dotnet-reportgenerator-globaltool --tool-path .\.tools
.\.tools\dotnet-coverage collect "dotnet run --project .\tests\Dotnetstore.MinimalApi.Api.WebApi.Tests\Dotnetstore.MinimalApi.Api.WebApi.Tests.csproj --configuration Release --no-build" -f cobertura -o .\artifacts\coverage\coverage.cobertura.xml
.\.tools\reportgenerator "-reports:.\artifacts\coverage\coverage.cobertura.xml" "-targetdir:.\artifacts\coverage-report" "-reporttypes:HtmlInline;MarkdownSummaryGithub;Badges"
```

## License

This project is licensed under the MIT License. See `LICENSE` for details.
