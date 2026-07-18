# Schoolera Implementation Baseline

Audit date: 2026-07-15  
Repository root: `D:\Test2\School\Project`  
Reference product (business/UX only, no asset copying): [Madares](https://madares.sa/)

This document is the authoritative pre-feature baseline for Prompts 2–26. It records what exists today, verified build/test results, and constraints that must be preserved.

---

## 1. Architecture (text diagram)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Browser                                                                 │
│  Production: same origin (API hosts SPA from wwwroot)                   │
│  Development: ng serve (5100/4200/4201) + API HTTP (5085) + proxy/CORS │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Schoolera.Api (ASP.NET Core 10, net10.0)                                │
│  Middleware: ExceptionHandling → Swagger* → HTTPS† → CORS* → AuthZ      │
│              → DefaultFiles/StaticFiles → MapControllers                │
│              → MapFallback /api/** (404) → MapFallback /swagger/** (404)│
│              → MapFallbackToFile(index.html)                            │
│  * Swagger/CORS from config; † HTTPS off in Development (proxy-friendly) │
│  Dev: NSwagClientService → SwaggerClient.service.ts (official FE contract) │
└───────────────┬───────────────────────────────┬─────────────────────────┘
                │ project refs                  │ hosts
                ▼                               ▼
┌───────────────────────────────┐   ┌─────────────────────────────────────┐
│ Schoolera.Application         │   │ Schoolera-SPA (Angular 22)          │
│  MediatR CQRS                 │   │  Feature *Api → NSwag Client        │
│  FluentValidation             │   │  Builds to ../wwwroot (production)  │
│  Result<T> envelope           │   └─────────────────────────────────────┘
│  DTOs, handlers, validators   │
└───────────────┬───────────────┘
                │ interfaces
                ▼
┌───────────────────────────────┐
│ Schoolera.Infrastructure      │
│  EF Core 10 + SQL Server      │
│  DbContext, migrations, seed  │
│  ISchoolRepository, UoW       │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│ Schoolera.Domain              │
│  Entities (School, taxonomies, catalog)   │
└───────────────────────────────┘

See also:

- [school-catalog.md](./school-catalog.md) for taxonomy relationships, public/admin APIs, and seed behavior.
- [school-onboarding.md](./school-onboarding.md) for SchoolOwner onboarding workflow, private documents, admin review APIs, and approval → unpublished School behavior.
- [school-portal.md](./school-portal.md) for approved-school portal management (ownership, school-scoped SchoolAdmin membership, media, team).
- [authentication.md](./authentication.md) for cookie auth, CSRF, roles, and policies (`SchoolOwnerOnly`, `PlatformAdminOnly`).
- [parent-dashboard.md](./parent-dashboard.md) for Parent profile, children, and dashboard.
- [admission-applications.md](./admission-applications.md) for admission applications (Parent APIs, School Portal review, Platform Admin monitoring).
- [cms-and-contact.md](./cms-and-contact.md) for lightweight CMS content, FAQs, homepage management, and public contact requests.

Tests: Schoolera.Tests (xUnit architecture + middleware tests)
```

**Layer rules (enforced by tests):**
- Controllers are thin; delegate to `ISender` (MediatR); return `Result<T>`.
- No validation, DTO definitions, or repository usage in controllers.
- Commands/queries live under `Application/{Feature}/Commands|Queries`.
- Handlers inject repositories + UoW (commands) or repositories only (queries).
- Persistence implementations live under `Infrastructure/Persistence`.

---

## 2. .NET toolchain and packages

| Item | Value |
|------|-------|
| SDK (verified) | **10.0.302** |
| Target framework | **net10.0** (all projects) |
| Solution file | `Schoolera.slnx` |
| EF CLI tool | `dotnet-ef` **10.0.9** (`dotnet-tools.json`) |

### Project references

| Project | References |
|---------|------------|
| `Schoolera.Api` | Application, Infrastructure |
| `Schoolera.Application` | Domain |
| `Schoolera.Infrastructure` | Application, Domain |
| `Schoolera.Tests` | Api, Application, Domain, Infrastructure |

### Key NuGet packages

**Schoolera.Api**
- MediatR 14.2.0
- Swashbuckle.AspNetCore 10.1.5
- Microsoft.OpenApi 2.9.0
- NSwag.CodeGeneration.TypeScript 14.7.1 (dev client generation only)
- Microsoft.EntityFrameworkCore.Design 10.0.9

**Schoolera.Application**
- MediatR 14.2.0
- FluentValidation.DependencyInjectionExtensions 12.1.1

**Schoolera.Infrastructure**
- Microsoft.EntityFrameworkCore.SqlServer 10.0.9
- Microsoft.Extensions.Options.ConfigurationExtensions 10.0.9

---

## 3. Backend patterns

### MediatR / validation / results
- Registration: `AddApplication()` registers validators, MediatR, and `ValidationBehavior<,>`.
- Validation failures throw `ValidationException` → middleware returns **400** with `Result<object>`.
- Unhandled exceptions → **500** with generic error message.
- API envelope: `Result<T> { succeeded, data, errors }`.

### Authentication / authorization
- **Not implemented.** No `AddAuthentication`, `UseAuthentication`, or `[Authorize]`.
- `UseAuthorization()` is registered but no auth scheme is configured.
- Angular has placeholder `AuthService` / `auth.guard` (client-side signal only).

### Exception middleware
- `ExceptionHandlingMiddleware` converts validation and unhandled exceptions to JSON `Result<object>`.

### Swagger / OpenAPI
- Swashbuckle: `/swagger`, `/swagger/v1/swagger.json`.
- Enabled when `Swagger:Enabled` is true, or Development default fallback.
- Registered **before** static files and SPA fallback.
- Unmatched `/swagger/**` returns API **404** (not `index.html`).

### NSwag (official Angular API contract)

```text
Backend DTOs and OpenAPI
    ↓
Automatic NSwag generation (Development ApplicationStarted)
    ↓
SwaggerClient.service.ts
    ↓
Angular feature-specific API services (e.g. SchoolsApi)
    ↓
Components/pages
```

| Item | Value |
|------|-------|
| Generator service | `Schoolera.Api/Services/NSwagClientService.cs` |
| Options | `NswagOptions` / `Nswag:*` in appsettings |
| Trigger | `app.Lifetime.ApplicationStarted` when Development and `Nswag:Enabled` |
| Output | `Schoolera-SPA/src/app/core/api-client/SwaggerClient.service.ts` |
| Schools generated client | `Client` class |
| Schools generated methods | `schoolsGET()`, `schoolsPOST(body?)` |
| Generated DTOs | `SchoolDto`, `CreateSchoolCommand`, `SchoolDtoResult`, `SchoolDtoIReadOnlyCollectionResult` |
| Base URL token | Generated `API_BASE_URL` (`InjectionToken`) |
| Manual edits | **Forbidden** |

**Base URL strategy:** Generated URLs are `baseUrl + "/api/..."`. Provide the generated `API_BASE_URL` as `''` (empty origin). Production uses same-origin relative `/api/...`. Development uses `proxy.conf.json` to forward `/api` (and `/swagger`) to the local API. Do not embed localhost in generated TypeScript.

**Git / CI:** `SwaggerClient.service.ts` is tracked in Git so angular builds/CI work without first starting the API. Development regenerates/updates it on API startup. Always regenerate after OpenAPI/DTO changes before consuming frontend work.

**Legacy `ApiClient`:** Still present at `core/http/api-client.ts` for potential non-OpenAPI or pending HTTP needs. Paths must be absolute-from-origin (for example `/api/...`). **No current feature service uses it** after Schools migration. Prefer NSwag for every endpoint represented in OpenAPI.

**Remaining hand-written models:** feature UI-only view models only (forms/filters). Do not duplicate NSwag API DTOs. Legacy `schools.models.ts` / create-school UI removed in Phase 1 readiness pass.

**Limitations:** Generated class name is currently `Client` (empty OpenAPI controller tag expansion of `{controller}Client`). Methods use verb suffixes (`schoolsGET` / `schoolsPOST`). Feature services isolate consumers from these names.

---

## 4. Database

| Item | Value |
|------|-------|
| Provider | **SQL Server** (EF Core) |
| Connection source | `ConnectionStrings:DefaultConnection` in appsettings |
| Server pattern | `Data Source=.` (local default instance) |
| Database name | `SchooleraDb` |
| Auth | Integrated Security |
| Resilience | `EnableRetryOnFailure` (5 retries, 10s max delay) |

**Do not commit or paste full connection strings with secrets in docs.** Current config uses Windows integrated auth (no password in file).

### Migrations

| Migration | Date | Changes |
|-----------|------|---------|
| `20260704130746_InitialCreate` | 2026-07-04 | Creates `Schools` table |
| `20260715234412_AddSchoolNameIndex` | 2026-07-15 | Index `IX_Schools_Name` for seed/lookup |

**Schema: `Schools`**
- `Id` uniqueidentifier PK
- `Name` nvarchar(200) required (+ non-unique index)
- `City` nvarchar(100) nullable
- `CreatedAtUtc` datetimeoffset required

Field lengths come from `Schoolera.Domain.Common.FieldLengthLimits` (shared by Domain, validators, EF config).

### Startup database behavior (`Database` section)

| Setting | appsettings.json (production-safe) | appsettings.Development.json |
|---------|------------------------------------|------------------------------|
| `ApplyMigrations` | **false** | **true** |
| `SeedData` | **false** | **true** |
| `Cors:AllowedOrigins` | `[]` (unused in Production) | Exact Angular `ng serve` origins (`5100`, `4200`, `4201`) |
| `HttpsRedirection:Enabled` | **true** | **false** (keeps HTTP `:5085` for the proxy) |

Lifecycle: scoped `DbContext` → `MigrateAsync` when enabled (failure stops startup) → seed when enabled and migration did not fail. Seed matches schools by stable **Name** (logs inserted/skipped). See `docs/development-database.md`.

### File storage foundation

| Item | Value |
|------|-------|
| Contract | `IFileStorage` / `StoreFileRequest` / `StoredFile` (Application) |
| Implementation | `LocalFileStorage` (Infrastructure) |
| Options | `FileStorage:*` (`StorageRoot`, `PublicRequestPath`, `MaxFileSizeBytes`, allow-lists) |
| Physical root | `App_Data/uploads` (API content root; outside Angular `wwwroot`) |
| Public URL prefix | `/uploads/...` (relative URLs only; no machine paths) |
| Validation | extension + content-type match, max size, empty reject, category sanitize, path traversal protection, image signatures (JPEG/PNG/WEBP) |
| Static files | Separate `UseStaticFiles` mapping; `MapFallback("/uploads/{**path}")` → API 404 |
| Git | `App_Data/uploads/**` ignored except `.gitkeep` |

No upload product endpoints yet — storage is ready for school logos/galleries/documents.

### Shared Application/Domain primitives (this foundation)

| Primitive | Location | Used by |
|-----------|----------|---------|
| `FieldLengthLimits` | Domain | School entity, CreateSchool validator, School EF config |
| `PagedRequest` / `PagedResult<T>` | Application | Unit tests + ready for list endpoints |
| `IFileStorage` | Application | LocalFileStorage + DI registration |

---

## 5. API endpoints (current)

Public school catalog (authoritative Phase 1 surface):

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/schools` | Public search/list (`PublicSchoolListItemDto` paged) |
| GET | `/api/schools/{slug}` | Published profile only |
| GET | `/api/schools/{slug}/related` | Related published schools |
| POST | `/api/schools/{slug}/contact-leads` | CSRF + rate limit |

**Removed from HTTP:** `POST /api/schools` (legacy `CreateSchoolCommand` endpoint). School creation for Phase 1 is via School Owner onboarding approval / Admin publication — not an anonymous public create. The Application `CreateSchoolCommand` / validator remain only for localization foundation unit tests.

See feature docs for Parent, School Portal, Admin, Admissions, and CMS endpoint tables.

---

## 6. Angular SPA

| Item | Value |
|------|-------|
| Location | `src/Schoolera.Api/Schoolera-SPA` |
| Angular | **22.x** (`@angular/*` ^22.0.0) |
| TypeScript | ~6.0.2 |
| Package manager | npm 11.13.0 (lock file present) |
| Structure | Standalone components, lazy feature routes, nested public layout |
| Selector prefix | `se` |
| Styling | SCSS per component + global `styles.scss` design tokens (RTL-ready) |
| Tests | Vitest via `@angular/build:unit-test` |
| Document | Runtime `lang`/`dir` via Transloco (`ar` RTL default, `en` LTR); IBM Plex Sans Arabic |
| Localization | Transloco (`@jsverse/transloco` 8.4) + locale + persist-lang; files in `public/i18n/` |

### Environments / API base URL

| File | `apiBaseUrl` |
|------|--------------|
| `environment.ts` (production) | `''` (empty; NSwag paths already include `/api/...`) |
| `environment.development.ts` | `''` + `proxy.conf.json` → `http://127.0.0.1:5085` |

Token: **NSwag-generated** `API_BASE_URL` from `SwaggerClient.service.ts`, provided in `app.config.ts`.  
`core/config/environment.token.ts` only re-exports that same token (no second identity).

### HTTP layer (active pattern)

```text
Component/page → SchoolsApi (feature) → Client (NSwag) → HttpClient → /api/...
```

- `core/api-client/SwaggerClient.service.ts` — generated (do not edit)
- `features/schools/data-access/schools.api.ts` — wraps NSwag public school search/profile/contact-lead methods
- Deprecated shared `core/http/api-client.ts` — **removed** in Phase 1 readiness pass
- `core/http/http-error.interceptor.ts` — logs HTTP failures

### Development workflow (proxy)

- Prefer the **`http`** launch profile (`http://localhost:5085`) or `dotnet run --urls http://127.0.0.1:5085`. Do not rely on the dual `https` profile (7076 + 5085) when using the Angular proxy — HTTPS redirection is disabled in Development (`HttpsRedirection:Enabled=false`) so HTTP API calls are not 307-redirected to a different HTTPS origin.
- `proxy.conf.json` wired in `angular.json` serve options.
- Proxies `/api` and `/swagger` to `http://127.0.0.1:5085`.
- Angular keeps relative `/api` (`API_BASE_URL` / `environment.apiBaseUrl` = `''`).
- API CORS policy `AngularDev` (Development only) reads exact origins from `Cors:AllowedOrigins` (includes `localhost`/`127.0.0.1` on `5100`, `4200`, `4201`) with `AllowCredentials()`. Never `AllowAnyOrigin()`. Production does not enable CORS — same-origin hosting only.

### Routing inventory

```text
PublicLayout (skip link + header + main + footer)
├── /                     HomePage (CMS overlay + Featured Schools API)
├── /schools              SchoolListPage
├── /schools/:slug        SchoolDetailsPage (published only)
├── /about|/how-it-works|/privacy|/terms|/sla  CmsStaticPage
├── /faq                  FaqPage
├── /contact              ContactPage
├── /auth/*               Login / register / verification / password flows
├── /parent/*             Parent portal
├── /school/*             Onboarding + School Portal
└── /admin/*              Platform Admin

AppShell (legacy scaffold ops routes — not Phase 1 product surface)
├── /dashboard
├── /students
├── /staff
├── /classes
```

See SPA `README.md` and [phase-1-qa.md](./phase-1-qa.md) for the authoritative Phase 1 route checklist.

| App route | Feature | Page(s) | Backend wired? |
|-----------|---------|---------|----------------|
| `/` | home | HomePage | CMS home + Featured Schools API |
| `/schools` | schools | SchoolListPage | Yes |
| `/schools/:slug` | schools | SchoolDetailsPage | Yes (published profile) |
| `/about`, `/how-it-works`, `/privacy`, `/terms`, `/sla` | public | CmsStaticPage | Yes (`/api/content/pages/{slug}`) |
| `/faq` | public | FaqPage | Yes |
| `/contact` | public | ContactPage | Yes |
| `/auth/**` | auth | Login/register/verification flows | Yes |
| `/parent/**` | parent | Dashboard/profile/children/applications | Yes |
| `/school/**` | portal/onboarding | School Portal + onboarding | Yes |
| `/admin/**` | admin | Platform Admin console | Yes |
| `/dashboard`, `/students`, `/staff`, `/classes` | scaffold | Placeholder list pages | No (not Phase 1 product) |
| `/**` | not-found | NotFoundPage | SPA |

### Shared UI (design system seeds)

- `shared/ui/button`
- `shared/ui/form-field`
- `shared/ui/data-table`
- `shared/ui/empty-state`
- `shared/ui/public-page-container`
- `shared/ui/page-hero`
- `shared/ui/breadcrumbs`
- `shared/ui/public-placeholder`
- `shared/pipes/empty-value.pipe`
- `shared/directives/autofocus.directive`
- `shared/validators/required-text.validator`

### Layout

- `core/layout/public-layout` — public marketing/discovery chrome
- `core/layout/public-header` — RTL responsive header + accessible mobile menu
- `core/layout/public-footer` — grouped Arabic footer (no `href="#"`)
- `core/layout/app-shell` — temporary operations shell (dashboards later)
- `core/layout/nav` — operations nav
- `core/layout/page-header` — page heading used by existing feature pages

### Accessibility baseline (public shell)

- Skip-to-content link targeting `main#main-content`
- Semantic `header` / `nav` / `main` / `footer`
- Keyboard-accessible mobile menu (`aria-expanded`, `aria-controls`, Escape to close, `inert` when closed)
- Visible focus styles via global tokens
- Route navigation focuses main content and scrolls to top (respects `prefers-reduced-motion`)
- Breadcrumbs use `aria-current="page"`; hidden on small screens
- Route `title` metadata for browser titles (Arabic)

### RTL / localization readiness

- **Permanent:** Arabic + English are required. See `docs/localization.md`.
- Default language Arabic with RTL; English uses LTR.
- Transloco runtime localization; language switcher in the public header; language persisted in `localStorage` (`schoolera.lang`).
- `Accept-Language` sent on API calls; API uses `UseRequestLocalization` with default culture `ar`.
- No Angular compile-time i18n and no ngx-translate.

### SPA build → wwwroot

- `angular.json` output: `../wwwroot` (production default configuration).
- MSBuild targets in `Schoolera.Api.csproj` run `npm ci` + `npm run build` on Release / `BuildSpa=true` / publish.
- `wwwroot` is gitignored except `.gitkeep`.

---

## 7. Build and test commands

> Results below include the original 2026-07-15 audit run plus NSwag adoption verification in this NSwag prompt. Re-run commands after local toolchain changes.

### .NET (from repo root)

```powershell
dotnet restore Schoolera.slnx
dotnet build Schoolera.slnx -c Debug
dotnet test Schoolera.slnx -c Debug --no-build
```

### Angular (from `src/Schoolera.Api/Schoolera-SPA`)

```powershell
npm ci
npm run build
```

**Node requirement (Angular 22):** `^22.22.3 || ^24.15.0 || >=26.0.0`  
**Project pin:** `Schoolera-SPA/.nvmrc` and `.node-version` → **`24.18.0`** (verified production build + unit tests on this pin).

### Regenerate NSwag client

```powershell
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --urls http://127.0.0.1:5085
```

With Development + `Nswag:Enabled=true`, `ApplicationStarted` refreshes `SwaggerClient.service.ts` from `/swagger/v1/swagger.json`.

---

## 8. Current blockers and limitations

### Closed in Phase 1 toolchain / readiness pass (2026-07-17)

- Compatible Node **24.18.0** project pin + portable session runtime; `npm ci`, `npm run build` → `wwwroot`, `ng test --watch=false` (**94** passed).
- `i18n:find` replaced with Angular-22-compatible `tools/i18n-find.mjs`; parity + find pass.
- NSwag client regenerated from live OpenAPI (no manual edits); feature wrappers use generated methods.
- Cookie auth, CSRF, parent/school/admin surfaces, CMS/contact, admissions, and public catalog are implemented (see module docs).
- Deprecated shared `ApiClient` removed; CreateSchool public POST removed.

### Remaining (release)

1. **Authenticated browser Journeys B–E** UI walkthrough incomplete in the readiness pass (API/CSRF smoke passed; browser credential automation declined).
2. **Full viewport matrix** (1440 / 1024 / 768 / 390 × AR+EN) not exhaustively signed off — see [phase-1-qa.md](./phase-1-qa.md).
3. Production **bundle budget warnings** (initial JS / home SCSS) — non-blocking.
4. Sass `@import` deprecation warnings — non-blocking.
5. Scaffold ops routes (`/dashboard`, `/students`, `/staff`, `/classes`) remain non-product placeholders.
6. NSwag generated client class is named `Client` (OpenAPI tags can improve naming later).
7. Runtime SEO only (no SSR) — documented limitation.

---

## 9. Risks to preserve (do not regress)

| Risk | Required behavior |
|------|-------------------|
| SPA fallback order | `MapControllers()` before `MapFallbackToFile("index.html")` |
| API 404 semantics | Unmatched `/api/**` must return API 404, not Angular HTML |
| Swagger isolation | Unmatched `/swagger/**` must not fall through to SPA |
| Same-origin production | Empty NSwag `API_BASE_URL` + generated `/api/...` paths |
| Uploads isolation | `/uploads/**` serves local files only; missing uploads → API 404 (not SPA) |
| Clean Architecture | No EF entities or Infrastructure types in API/Application surface |
| Result envelope | Controllers return `Result<T>`; errors via middleware |
| Migration safety | Do not delete migrations; additive changes only unless explicitly scoped |
| Production DB defaults | Base config keeps `ApplyMigrations`/`SeedData`/`Swagger` false unless overridden |
| NSwag source of truth | Never manually edit `SwaggerClient.service.ts`; feature services wrap clients |

---

## 10. Dependency-aware checklist (Prompts 2–26)

Use this order to avoid rework. Each item depends on all items above it unless marked optional. **New endpoints must ship with regenerated NSwag clients and feature-service wrappers.**

| Prompt | Focus | Depends on | Exit criteria |
|--------|-------|------------|---------------|
| **1** | Baseline audit (this doc) | — | Baseline doc + green .NET tests |
| **1b** | NSwag official contract adoption | 1 | Schools via NSwag; Cursor/Claude rules; tracked generated file |
| **1c** | Shared backend foundation | 1, 1b | Prod-safe DB flags; migrate/seed lifecycle; file storage; paging constants |
| **1d** | Public Angular RTL shell | 1 | PublicLayout + public routes + a11y mobile nav + Angular 404 |
| **1e** | Localization (ar/en Transloco) | 1d | Permanent AR/EN; Transloco; docs/rules; Accept-Language |
| **2** | Toolchain / CI baseline | 1, 1b | Node compatible; Angular build to wwwroot succeeds; client present |
| **3** | Config hardening | 1 | Production vs Development settings for Swagger, DB, NSwag |
| **4** | School contract polish | 1b, 2 | OpenAPI tags/client naming optional; e2e list/create green |
| **5** | Shared API error handling UX | 4 | Consistent UI for generated `Result` failures |
| **6** | Authentication backend | 3 | Login/session or JWT, `[Authorize]` pattern |
| **7** | Authentication frontend | 6 | Login route, guard; NSwag client regen if auth endpoints added |
| **8** | Arabic RTL polish / marketing homepage | 1d, 2 | Full homepage content beyond shell intro |
| **9** | Localization hooks (minimal) | 8 | i18n-ready structure |
| **10** | Authenticated app shells | 1d, 7 | Parent/school/admin layouts without public chrome |
| **11** | Dashboard (real data widgets) | 4, 10 | Metrics via feature services → NSwag |
| **12** | Schools list UX polish | 4, 10 | Table/filters using `SchoolDto` |
| **13** | School create/edit flows | 4, 12 | Forms aligned to generated commands |
| **14** | School details | 4, 12 | GET-by-id endpoint → regenerate NSwag → feature service |
| **15** | Students domain (backend) | 6, 4 | CQRS + OpenAPI |
| **16** | Students feature (frontend) | 15, 10 | Regen client + `StudentsApi` wrapper |
| **17** | Staff domain (backend) | 15 | Same pattern |
| **18** | Staff feature (frontend) | 17, 10 | Regen client + `StaffApi` |
| **19** | Classes domain (backend) | 15 | Classes linked to schools |
| **20** | Classes feature (frontend) | 19, 10 | Regen client + `ClassesApi` |
| **21** | Cross-cutting validation messages (AR) | 8, 9 | Arabic validation/error copy |
| **22** | Design system expansion | 10 | Shared UI reuse rules |
| **23** | E2E/smoke automation | 2, 4, 7 | Scripted smoke |
| **24** | Publish pipeline | 2, 3 | `dotnet publish` includes wwwroot SPA |
| **25** | Performance / accessibility pass | 10–20 | Responsive + a11y |
| **26** | Release readiness review | 1–25 | Checklist sign-off |

---

## 11. Manual verification steps

1. Ensure Node meets Angular 22 engine range; run `npm ci && npm run build` in `Schoolera-SPA`.
2. `dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --urls http://127.0.0.1:5085`
3. Confirm `/swagger/v1/swagger.json` returns 200 and NSwag refreshes `SwaggerClient.service.ts`.
4. Confirm generated file has Schools operations and **no** embedded localhost origins.
5. Confirm `GET /api/unknown` returns 404 (not HTML); `/schools` falls back to `index.html`.
6. Dev UI: `npm start -- --port 4201 --host 127.0.0.1` (proxy forwards `/api`).

---

## 12. Corrections applied after baseline

### NSwag adoption
- Adopted NSwag as the official Angular API contract for Schools.
- Migrated `SchoolsApi` to generated `Client.schoolsGET` / `schoolsPOST`.
- Unified `API_BASE_URL` to the generated token with empty origin + Angular proxy.
- Added `.cursor/rules/00-schoolera-core.mdc` and root `CLAUDE.md`.

### Shared backend foundation
- Production-safe `Database` / `Swagger` defaults in base `appsettings.json`.
- Hardened migrate→seed startup logging and fail-fast on migration errors.
- Idempotent seed by school **Name**.
- Added `FieldLengthLimits`, paging models, `IFileStorage` + local uploads.
- Migration `20260715234412_AddSchoolNameIndex`.
- Docs: `docs/development-database.md`.

### Latest verification (foundation prompt)
- `dotnet restore/build`: succeeded
- `dotnet test`: **38/38 passed**
- Development API smoke: Swagger 200, `/api/schools` 200, `/api/unknown` 404, `/swagger/unknown` 404, `/uploads/missing-file.jpg` 404, `/schools` → HTML; second startup seed `Inserted=0, Skipped=3`
- `npm run build`: **Failed** — Node `v24.13.0` below Angular requirement

### Public Angular shell
- Nested `PublicLayout` with Arabic RTL header/footer; homepage intro; marketing/auth placeholders; Angular 404.
- Schools list/create preserved under public layout; details route renamed to `:slug` (placeholder only).
- Operations modules remain on separate `AppShell` (no public chrome).
- Added shared public UI: page container, page hero, breadcrumbs, public placeholder.
- Focused Vitest coverage for layout, nav, mobile menu Escape, footer hrefs, 404, public routes.

### Latest verification (public shell prompt)
- Node: `v24.13.0` / npm `11.6.2` — **below** Angular 22 engine range (`^22.22.3 || ^24.15.0 || >=26`)
- `npm ci`: succeeded (with EBADENGINE warnings)
- `npm run build` / `npm test`: **failed** — Angular CLI refused to run on Node `v24.13.0`
- `dotnet restore/build`: succeeded; `dotnet test`: **38/38 passed**
- Development API smoke (placeholder `wwwroot/index.html`, not a fresh Angular build):
  - Swagger UI + swagger.json **200**
  - NSwag: client confirmed up to date
  - `/api/schools` **200**
  - `/api/unknown`, `/swagger/unknown`, `/uploads/missing-file.jpg` → **404** and **not HTML**
  - `/`, `/schools`, `/schools/example-school`, `/about`, unknown SPA path → **200** HTML fallback
- Browser-level Angular client routing / 404 page rendering **not verified** (no compatible Node build output)
### Localization (ar/en)
- Adopted Transloco 8.4 with `public/i18n` root + `auth`/`schools`/`admin` scopes.
- Language switcher, document `lang`/`dir`, persist-lang, Accept-Language interceptor.
- Backend `UseRequestLocalization` default `ar`; localized unexpected API errors via `ApiMessages` resources.
- Docs: `docs/localization.md`; rules updated in `.cursor/rules` and `CLAUDE.md`.
- Permanent decision: Arabic + English required for Phase 1 (supersedes older English-optional wording).

### Localization completion pass
- Strengthened `DocumentLanguageService` (locale `ar-EG`/`en-US`, invalid persist ? Arabic, direction/locale signals).
- Language switcher in desktop header + mobile drawer (closes menu on change).
- `Result<T>.ErrorCodes` + `ErrorCodes` constants; localized validation + API messages.
- RequestLocalization default `ar-EG`; Accept-Language cultures `ar`/`en`/`ar-EG`/`en-US`.
- `LocaleFormatService`, `se-bilingual-field-group`, `/dev/bilingual-fields` showcase.
- Scripts: `i18n:check` / `i18n:validate` / `i18n:all`; full `docs/localization.md`.
- Backend tests: **48 passed** after localization foundation tests.

### Public homepage (production marketing page)
- Replaced the lightweight intro with a full bilingual homepage at `/` inside the existing `PublicLayout` (header/footer not duplicated).
- Sections: hero, trust/value, parent benefits, school benefits, Parent/School journey tabs, featured schools preview, non-numeric platform highlights, FAQ accordion preview, final CTA.
- Featured schools: `HomePage` → `SchoolsApi.getSchools()` → NSwag `Client.schoolsGET()`; shows up to 4 schools using only `id`, `name`, `city`; links to `/schools/:id` (no fabricated slug/ratings/fees/logos).
- Loading skeleton, empty state, and error + retry for the featured preview.
- Platform “stats” use honest non-numeric highlights (no invented production metrics).
- FAQ content is static Transloco copy structured for a later CMS (Prompt 17); accordion is accessible (`aria-expanded`, regions).
- SEO: runtime `title`, meta description, and basic Open Graph tags from `home.meta.*` (crawler limits without SSR remain).
- Original local asset: `public/assets/home/hero-education.svg`.
- Translation keys under root `home.*` in `public/i18n/{ar,en}.json` (no new Transloco scope).
- Focused Vitest suite: `home-page.spec.ts`.
- No auth, school-domain expansion, CMS, or UI framework added.

### Latest verification (homepage prompt)
- Node: `v24.13.0` / npm `11.6.2` — **below** Angular 22 engine range (`^22.22.3 || ^24.15.0 || >=26`)
- `npm run i18n:check`: **passed** (root 214 keys + scopes)
- `npm run i18n:validate`: parity **passed**; `i18n:find` **failed** (pre-existing keys-manager parse error on `angular.json`)
- `npm run build` / `npm test`: **failed** — Angular CLI refused Node `v24.13.0`
- `dotnet restore/build`: succeeded; `dotnet test`: **48/48 passed**
- Development API smoke on existing `http://127.0.0.1:5085`:
  - Swagger UI + swagger.json **200**
  - `/api/schools` **200** (Accept-Language ar/en)
  - `/api/unknown`, `/swagger/unknown`, `/uploads/missing-file.jpg` → **404** (not HTML)
  - `/`, `/schools` → **200** HTML SPA fallback
- Browser-level Angular homepage rendering **not verified** (no compatible Node build)

### Authentication (Phase 1)
- ASP.NET Core Identity + HttpOnly application cookie; CSRF on state-changing auth POSTs.
- Roles: Parent, SchoolOwner, SchoolAdmin, PlatformAdmin, SupportAgent.
- Public registration: Parent and SchoolOwner only.
- Policies: `ParentOnly`, `SchoolPortal`, `PlatformAdminOnly`, `SupportOrAdmin`.
- Endpoints: `/api/auth/*`; portal probes `/api/portal/*`.
- Migration `20260716081828_AddIdentityAndAuth`.
- Identity failures map `IdentityError.Code` → stable `auth.*` codes (`IdentityErrorMapper`); never return raw Identity English descriptions for client branching.
- Email verification: hashed 6-digit codes in `VerificationCodes`; delivery via `ITransactionalEmailSender` (`Email:Mode` = `DevelopmentLog` or `Smtp`). See `docs/authentication.md`.
- Angular auth feature: pages, guards, `AuthApi`/`AuthService`, form error summary, field errors, `ToastService`, verify-page masked email + resend cooldown, `public/i18n/auth`.
- Docs: `docs/authentication.md`.

### Parent dashboard / profile / children (Phase 1 foundation)
- Status: **implemented**. See [parent-dashboard.md](./parent-dashboard.md).
- Policy: `ParentOnly` on `/api/parent/**`.
- Entities: `ParentProfile` (extension fields only; names/phone/language on Identity), `ChildProfile` (masked identity + HMAC lookup + Data Protection ciphertext).
- Duplicate identity scope: **within Parent** only (`ParentUserId` + hash). No platform-wide national-ID uniqueness rule is documented.
- Dashboard: real child counts; application counts and parent-visible recent activity when admissions exist (see Admission Applications below).
- Angular: `ParentLayout` under `/parent/*`; Transloco scope `parent`; `ParentApi` wraps NSwag `Client`.
- Migration: `20260716184059_AddParentAndChildProfiles`.
- No payments, school messaging, or medical records.

### Admission Applications (Parent + School review + Admin monitoring)
- Status: **implemented** (backend + Parent/School Portal/Admin Angular). See [admission-applications.md](./admission-applications.md).
- Parent APIs: `/api/parent/admission-applications/**` (`ParentOnly`); create/update/submit/cancel draft attachments; ownership-safe 404.
- School Portal APIs: `/api/school-portal/schools/{schoolId}/applications/**` (`SchoolPortal` + `ISchoolPortalAccess`); start-review / accept / reject; attachment download.
- Admin APIs: `/api/admin/admission-applications/**` (`PlatformAdminOnly`, read-only list/detail + CSV export).
- Reuses `ChildProfile`; statuses Draft/Submitted/UnderReview/Accepted/Rejected/Cancelled (`ContractSent`/`Paid` absent).
- Transitions: `AdmissionTransitionPolicy.TryValidateSchoolTransition` (Submitted→UnderReview→Accepted/Rejected).
- Migration: `20260716192657_AddAdmissionApplications` (no new migration for review/monitoring).
- Angular: `/parent/applications/**`, `/school/:schoolId/applications/**`, `/admin/applications/**`.
- Notifications: **pending** (history authoritative; no email/SMS/bus).
- Public profile offerings expose `educationalStageId` (additive; no new migration).

### Latest verification (authentication prompt)
- Node: `v24.13.0` / npm `11.6.2` — **below** Angular 22 engine range
- `npm ci`: succeeded
- `npm run i18n:check`: **passed** (auth scope 98 keys)
- `npm run i18n:validate`: parity **passed**; `i18n:find` **failed** (pre-existing keys-manager `angular.json` parse error)
- `npm run build` / `npm test`: **failed** — Angular CLI refused Node `v24.13.0`
- `dotnet restore/build`: succeeded; `dotnet test`: **50/50 passed**
- EF migrations: `InitialCreate`, `AddSchoolNameIndex`, `AddIdentityAndAuth` (applied on local DB)
- Browser-level Angular auth **not verified** (Node blocker)

### School Portal (approved school management)
- Status: **implemented** (backend + Angular portal shell/pages). See [school-portal.md](./school-portal.md).
- Ownership: `School.OwnerUserId`; school-scoped `SchoolTeamMember` (`SchoolAdmin` only).
- APIs: `/api/school-portal/**` (dashboard, profile, branches, offerings, fees, facilities, media, services, team, **admission applications review**).
- Dashboard: `admissionsAvailable = true` with submitted/underReview/accepted/rejected/totalActive/recentSubmitted counts.
- Migration: `20260716161426_AddSchoolPortal` (+ admission tables from `AddAdmissionApplications`).
- Angular: dedicated `SchoolPortalLayout` under `/school/:schoolId/*`; onboarding routes preserved first; **Applications** nav + list/detail routes.
- NSwag: portal endpoints in `SwaggerClient.service.ts`; feature facade `SchoolPortalApi` (HttpClient for upload progress and attachment download).
- Transloco scope `portal` (AR/EN parity).
- Out of scope (confirmed): onboarding redo, publication from portal, contracts, payments, CRM, CMS, search UI, admission notifications.

### Platform Admin admission monitoring
- Status: **implemented** (read-only backend + Angular). See [platform-admin.md](./platform-admin.md) and [admission-applications.md](./admission-applications.md).
- APIs: `GET /api/admin/admission-applications`, detail, CSV export (`AdminAdmissionApplicationsExportController`, max 5000 rows).
- Dashboard: `admissionsAvailable = true` with draft/submitted/underReview/accepted/rejected/cancelled/today/pendingSchoolReview counts.
- Angular: `/admin/applications`, `/admin/applications/:applicationId`; `AdminPlatformApi` wraps NSwag + CSV HttpClient.
- **Not implemented:** admin force review actions, admin attachment download.

### Public school search (Phase 1)
- Extended existing `GET /api/schools` (no second endpoint). See [school-catalog.md](./school-catalog.md).
- Filters: search, city/district, curricula (ANY), facilities (ALL), stage/grade, type/gender, admissionOpen, EGP tuition range, academic year, nearest coords.
- Sort: relevance | name-asc | name-desc | lowest-fee | highest-fee | newest | nearest.
- Migration: `20260716181043_AddPublicSchoolSearchIndexes`.
- Angular `/schools` URL-driven search UI; homepage `getFeaturedSchools(4)` preserved.
- Transloco scope `schools` (85 keys AR/EN).

### Public school profile (Phase 1)
- Extended existing `GET /api/schools/{slug}` (no second profile endpoint). See [public-school-profile.md](./public-school-profile.md).
- Additive: `GET /api/schools/{slug}/related`, `POST /api/schools/{slug}/contact-leads` (CSRF + `schools-contact-lead` rate limit).
- Entities/migration: `SchoolContactLead`, `SchoolProfileViewDaily` → `20260716182358_AddPublicSchoolProfileLeadsAndViews`.
- Best-effort in-process profile-view Channel + BackgroundService (not distributed analytics).
- Angular `/schools/:slug` full profile UI; Favorites flag default off; Apply CTA when admissions feature + open offerings (see Admission Applications).
- Runtime SEO/JSON-LD via `SeoService`; `environment.publicSiteBaseUrl` for canonical.

### CMS and contact (Phase 1)
- Lightweight bilingual CMS for static pages, ordered FAQs, and structured homepage content; no page builder.
- Public APIs: `/api/content/pages/{slug}`, `/api/content/faqs`, `/api/content/home`, and CSRF-protected `POST /api/contact`.
- Platform Admin APIs: `/api/admin/cms/**` and `/api/admin/contact-requests/**`; Angular routes under `/admin/cms/**` and `/admin/contact-requests/**`.
- Server-side Ganss.Xss sanitization restricts managed HTML to the documented safe subset.
- Contact submissions use the `contact-submit` rate limit, `Website` honeypot, and a two-minute duplicate cooldown.
- Idempotent `CmsContentSeeder`; migration `20260716202921_AddCmsFaqHomepageAndContact`.
- See [cms-and-contact.md](./cms-and-contact.md) for endpoint, publication, privacy, audit, SEO, and limitation details.

### Phase 1 status (release readiness)

| Area | Status |
|------|--------|
| Public CreateSchool HTTP (`POST /api/schools`) | **Removed** from public API; Angular `/schools/new` and create-school UI removed. Application `CreateSchoolCommand` / validators remain for localization foundation tests only. |
| CMS static pages / FAQ / homepage / contact | **Live** (public + Platform Admin) |
| Catalog / portal / admissions / auth | Implemented (see sections above) |
| Development seed fixtures | Extended: second parent child, Unpublished/Suspended demo schools, Accepted/Rejected admissions, `CNT-DEMO-*` contacts. Onboarding Submitted/ChangesRequested **not** auto-seeded (manual in [phase-1-qa.md](./phase-1-qa.md)). |
| Integration readiness tests | `Phase1ReleaseReadinessTests` (CreateSchool absence, SPA fallbacks, CSRF, unpublished slug, start-review smoke) |
| Angular production build / Vitest | **Pending compatible Node** (`^22.22.3 \|\| ^24.15.0 \|\| >=26`). Node `v24.13.0` blocks Angular CLI. Backend `dotnet test` is the primary automated gate until Node is upgraded. |
| **Release classification** | **PARTIALLY READY** — backend + HTTP smoke verified; Angular production build and browser journeys blocked by Node. See [phase-1-qa.md](./phase-1-qa.md). |

QA checklist and sign-off: [phase-1-qa.md](./phase-1-qa.md).

