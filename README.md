# Schoolera

Schoolera is a clean-architecture school discovery and admissions platform: .NET 10 API, EF Core + SQL Server, and an Angular 22 SPA hosted by ASP.NET Core.

## Prerequisites

| Tool | Requirement |
|------|-------------|
| .NET SDK | **10.x** |
| Node.js | `^22.22.3 \|\| ^24.15.0 \|\| >=26` (Angular 22 engines). Project pin: **`24.18.0`** via `Schoolera-SPA/.nvmrc` and `.node-version` |
| npm | Compatible with the Node version above |
| SQL Server | Local or remote instance |

### Node version for Angular

Use a Node runtime that satisfies Angular 22 engines. The SPA declares **`24.18.0`** in `.nvmrc` / `.node-version`.

If your machine default is older (for example `v24.13.0`), activate a compatible runtime **for the shell only** (nvm/fnm/volta or a portable Node). Do not treat SPA production builds as verified on an unsupported Node. See [docs/phase-1-qa.md](docs/phase-1-qa.md).

### Known Node blocker

If your Node is below the engine range, Angular CLI will refuse `npm run build` / `npm test`. Backend build and `dotnet test` still run.

## Structure

- `src/Schoolera.Api` — ASP.NET Core API + static SPA host
- `src/Schoolera.Api/Schoolera-SPA` — Angular application
- `src/Schoolera.Api/wwwroot` — production Angular output
- `src/Schoolera.Application` — MediatR CQRS, validators, DTOs
- `src/Schoolera.Domain` — entities and domain rules
- `src/Schoolera.Infrastructure` — EF Core, Identity, seed, file storage
- `tests/Schoolera.Tests` — architecture + integration tests
- `tests/Schoolera.E2E` — Playwright .NET browser regression

## Documentation

| Doc | Topic |
|-----|--------|
| [docs/implementation-baseline.md](docs/implementation-baseline.md) | Architecture baseline and Phase 1 status |
| [docs/phase-1-qa.md](docs/phase-1-qa.md) | **Phase 1 QA checklist and sign-off** |
| [docs/e2e-playwright.md](docs/e2e-playwright.md) | **Playwright .NET E2E + CI required check** |
| [docs/github-ci-setup.md](docs/github-ci-setup.md) | GitHub secret + branch protection for E2E |
| [docs/localization.md](docs/localization.md) | Arabic + English (Transloco) |
| [docs/authentication.md](docs/authentication.md) | Cookie auth, CSRF, roles |
| [docs/development-database.md](docs/development-database.md) | Migrations and seed |
| [docs/cms-and-contact.md](docs/cms-and-contact.md) | CMS + contact |
| [docs/admission-applications.md](docs/admission-applications.md) | Admissions |
| [docs/school-portal.md](docs/school-portal.md) | School portal |
| [docs/platform-admin.md](docs/platform-admin.md) | Platform admin |

## Auth seed password (user secrets)

Never commit passwords. Set the Development seed password with user secrets:

```powershell
cd src/Schoolera.Api
dotnet user-secrets set "Auth:SeedUsers:DefaultPassword" "<your-dev-password>"
```

If unset in Development, configuration may fall back to a local default in `appsettings.Development.json`. Prefer user secrets for shared machines.

## Demo users (emails only)

Seeded when `Database:SeedData=true` (Development default):

| Role | Email |
|------|-------|
| Parent | `parent@schoolera.local` |
| School owner | `schoolowner@schoolera.local` |
| School admin | `schooladmin@schoolera.local` |
| Platform admin | `admin@schoolera.local` |
| Support agent | `support@schoolera.local` |

Password: only via `Auth:SeedUsers:DefaultPassword`.

Useful slugs: `cairo-international-school` (published), `demo-unpublished-school`, `demo-suspended-school`.

## Database — migrate, seed, run

Connection string example (local SQL Server):

```
Data Source=.;Initial Catalog=SchooleraDb;Integrated Security=True;TrustServerCertificate=True
```

```json
"Database": {
  "ApplyMigrations": true,
  "SeedData": true
}
```

Defaults: both `false` in `appsettings.json`; both `true` in `appsettings.Development.json`.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj --startup-project src/Schoolera.Api/Schoolera.Api.csproj
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http
```

API: `http://127.0.0.1:5085`. Seed is idempotent and additive (catalog, CMS, admissions fixtures).

## Hosting model

1. Angular builds into `Schoolera.Api/wwwroot`.
2. Controllers map first (`MapControllers`).
3. Unmatched `/api/**`, `/swagger/**`, and `/uploads/**` return API/file **404** (not Angular `index.html`).
4. Remaining SPA routes fall back to `index.html`.
5. Production calls use relative `/api/...` (same origin). Do not hardcode localhost in production or generated clients.

### Swagger

- Development: `/swagger`, `/swagger/v1/swagger.json` enabled by default.
- Production: set `"Swagger": { "Enabled": true }` when needed.

### NSwag (official Angular API contract)

When `Nswag:Enabled` is true (Development), startup regenerates:

`src/Schoolera.Api/Schoolera-SPA/src/app/core/api-client/SwaggerClient.service.ts`

Do not edit that file manually. Feature services (for example `SchoolsApi`) wrap the generated `Client`.

There is **no** public `POST /api/schools` CreateSchool HTTP endpoint in Phase 1; schools are created via onboarding approval / portal management.

### Angular proxy (Development)

```powershell
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http
cd src/Schoolera.Api/Schoolera-SPA
npm start -- --port 5100
```

`proxy.conf.json` forwards `/api` and `/swagger` to `http://127.0.0.1:5085`. Keep generated `API_BASE_URL` empty.

## Localization

Arabic (`ar` / RTL, default) and English (`en` / LTR) are mandatory. See [docs/localization.md](docs/localization.md).

```powershell
cd src/Schoolera.Api/Schoolera-SPA
npm run i18n:check
npm run i18n:validate
```

## Validation commands

```powershell
dotnet build Schoolera.slnx -c Debug
dotnet test tests/Schoolera.Tests/Schoolera.Tests.csproj -c Debug
# Browser E2E (API + SPA must be running — see docs/e2e-playwright.md)
$env:E2E_BASE_URL = "http://localhost:5100"
$env:E2E_SEED_PASSWORD = "<seed-password>"
dotnet test tests/Schoolera.E2E/Schoolera.E2E.csproj -c Debug --settings tests/Schoolera.E2E/.runsettings
cd src/Schoolera.Api/Schoolera-SPA
npm run build
npm test -- --watch=false
```

Phase 1 QA filter (subset):

```powershell
dotnet test tests/Schoolera.Tests/Schoolera.Tests.csproj -c Debug --filter "FullyQualifiedName~Phase1ReleaseReadinessTests|FullyQualifiedName~CmsAndContactTests|FullyQualifiedName~SchoolAdmissionReviewTests"
```

## Phase 1 QA

Full practical checklist, journeys, CSRF/fallback checks, and sign-off fields: **[docs/phase-1-qa.md](docs/phase-1-qa.md)**.

Playwright browser automation: **[docs/e2e-playwright.md](docs/e2e-playwright.md)**.

