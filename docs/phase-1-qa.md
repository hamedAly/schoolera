# Phase 1 QA checklist

Practical release-readiness checklist for Schoolera Phase 1. Use alongside [implementation-baseline.md](./implementation-baseline.md), [localization.md](./localization.md), and [authentication.md](./authentication.md).

## Environment

| Item | Expectation |
|------|-------------|
| .NET SDK | **10.x** (verified on **10.0.302**) |
| Node.js | `^22.22.3 \|\| ^24.15.0 \|\| >=26` (Angular 22 engines) |
| Project pin | `src/Schoolera.Api/Schoolera-SPA/.nvmrc` and `.node-version` → **`24.18.0`** |
| SQL Server | Local instance with connection string in `Schoolera.Api` config / user secrets |
| Database flags | Development: `Database:ApplyMigrations=true`, `Database:SeedData=true` |

### Setup (compatible Node)

Do **not** change the machine-wide Node default without approval. Prefer an already-installed supported LTS, or a project-local portable runtime.

```powershell
cd D:\Test2\School\Project\src\Schoolera.Api\Schoolera-SPA
# If using nvm-windows / fnm / volta, select the version in .nvmrc (24.18.0)
node --version   # must satisfy Angular engines
npm ci
```

On this verification host, machine Node was `v24.13.0` (below engines). A portable Node **`v24.18.0`** under gitignored `.tools/node-v24.18.0-win-x64` was used for the session PATH only.

### User secrets (password only — never commit)

```powershell
cd src/Schoolera.Api
dotnet user-secrets set "Auth:SeedUsers:DefaultPassword" "<your-dev-password>"
```

Development default when unset: see `appsettings.Development.json` (`Auth:SeedUsers:DefaultPassword`). Do **not** put passwords in this doc or in git.

## Demo users (emails only)

| Role | Email |
|------|-------|
| Parent | `parent@schoolera.local` |
| School owner | `schoolowner@schoolera.local` |
| School admin | `schooladmin@schoolera.local` |
| Platform admin | `admin@schoolera.local` |
| Support agent | `support@schoolera.local` |

Password: from `Auth:SeedUsers:DefaultPassword` only.

### Useful seeded fixtures (after seed)

- Published school slug: `cairo-international-school`
- Unpublished / suspended (public 404): `demo-unpublished-school`, `demo-suspended-school`
- Parent: two children; admission apps include Draft / Submitted / UnderReview plus Accepted / Rejected when slots allow
- Contact demos: `CNT-DEMO-NEW`, `CNT-DEMO-INREVIEW`, `CNT-DEMO-RESOLVED`
- **Onboarding Submitted / ChangesRequested:** not auto-seeded (domain complexity). Create manually via SchoolOwner wizard + Platform Admin review UI.

## Run / migrate / seed

```powershell
cd D:\Test2\School\Project
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj --startup-project src/Schoolera.Api/Schoolera.Api.csproj
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http
```

API HTTP: `http://127.0.0.1:5085`. Swagger: `/swagger`. NSwag regenerates `SwaggerClient.service.ts` when `Nswag:Enabled` in Development.

Angular (compatible Node):

```powershell
cd src/Schoolera.Api/Schoolera-SPA
npm ci
npm start -- --port 5100
```

Proxy: `proxy.conf.json` → API `:5085`. Keep `API_BASE_URL` empty. Prefer `http://localhost:5100` for the Vite host (binds localhost).

## Journeys A–E (task mapping)

Use these names for release sign-off (maps to product flows below):

| Task journey | Focus |
|--------------|--------|
| **A — Visitor** | Homepage → search/filters → profile → contact (AR/EN) |
| **B — Parent** | Login → dashboard → child → draft/submit → track |
| **C — School owner / onboarding** | Register/verify → onboarding → admin review → portal catalog |
| **D — School admission review** | Portal applications → review → accept/reject → parent visibility |
| **E — Platform Admin** | Dashboard → onboarding/schools/users/taxonomies/apps/CMS/FAQ/contact/audit |

### A — Visitor (browser + API)

1. `/` CMS/fallback homepage, featured schools, FAQ teaser.
2. `/schools` search, filters, query sync, pagination, empty/error.
3. `/schools/cairo-international-school` profile, gallery, fees, contact modal.
4. `/contact` form + consent.
5. Language switcher: `lang`/`dir` update without full reload.

### B — Parent

1. Login as `parent@schoolera.local` → `/parent/dashboard`.
2. Children (expect **two**), masked identity.
3. Applications: draft create once, refresh reload, attachments, submit, timeline.

### C — School owner / onboarding

1. Owner login or fresh registration + DevelopmentLog verification.
2. `/school/onboarding` persist + private documents.
3. Admin review / changes requested / approval idempotency.
4. Portal catalog (branches, stages, fees, gallery, publish).

### D — School admission review

1. Owner/admin → applications list/detail.
2. Start review → Accept / Reject (reason required on reject).
3. Parent sees status/timeline; internal notes hidden.
4. CSRF: state-changing POSTs fail without `X-XSRF-TOKEN`.

### E — Platform Admin

1. `admin@schoolera.local` → `/admin/dashboard`.
2. Schools status actions; users; taxonomies CRUD; admissions monitoring + CSV.
3. CMS pages/FAQ/home; contact `CNT-DEMO-*`; audit without sensitive fields.

## CSRF / SPA fallback / localization

| Check | Pass criteria | Verified (2026-07-17) |
|-------|----------------|------------------------|
| CSRF contact missing token | `POST /api/contact` → **400** | Pass |
| CSRF contact invalid token | `POST /api/contact` + bad `X-XSRF-TOKEN` → **400** | Pass |
| CSRF contact valid token | `POST /api/contact` + cookie + header → **200** | Pass |
| CSRF school start-review missing | Owner `POST .../start-review` without token → **400** | Pass |
| CSRF admin mutation missing | Authenticated admin POST without token → **400** | Pass |
| API unknown | `GET /api/unknown` → **404**, not HTML | Pass |
| Swagger unknown | `GET /swagger/unknown` → **404** | Pass |
| Uploads missing | `GET /uploads/missing-file-phase1-qa.jpg` → **404** | Pass |
| SPA proxy unknown API | `GET http://localhost:5100/api/unknown` → **404** | Pass |
| CreateSchool | `POST /api/schools` → **404** or **405** | Pass |
| Unpublished / suspended profile | public GET → **404** | Pass |
| Wrong-role | Parent `GET /api/admin/dashboard` → **403** | Pass |
| i18n | `npm run i18n:check` + `i18n:validate` | Pass |
| Branching | UI branches on `errorCodes` | Preserved |

## Responsive / a11y (manual)

- Mobile nav / language switcher / forms usable near **390×844** (verified on contact EN: hamburger present; `overflowX=false` at 390×844).
- Skip link, single H1 on home/search/profile/contact (spot-checked).
- RTL Arabic + LTR English language switch verified on contact.
- Full matrix **1440×900 / 1024×768 / 768×1024 / 390×844 × AR+EN** for all five journeys: **not fully completed** in this pass (see Remaining).

Screenshot captured: `docs/qa-evidence/phase1/contact-en-390x844.png`.

## Automated verification (executed)

```powershell
# Backend (from repo root)
dotnet restore Schoolera.slnx
dotnet build Schoolera.slnx -c Debug
dotnet test Schoolera.slnx -c Debug --no-build
# Result: 310 passed, 0 failed, 0 skipped (~22s)

dotnet ef migrations list --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj --startup-project src/Schoolera.Api/Schoolera.Api.csproj
# No new migration for this task

# Angular (Node 24.18.0)
cd src/Schoolera.Api/Schoolera-SPA
npm ci
npm run i18n:check
npm run i18n:validate
npm run build          # → Schoolera.Api/wwwroot
npx ng test --watch=false
# Result: 34 files, 94 tests passed
```

### i18n tooling note

Official `@jsverse/transloco-keys-manager` `find` is incompatible with Angular 22 (hang / peer on `@angular/compiler < 22`). Repository uses `tools/i18n-find.mjs` via `npm run i18n:find` while preserving parity validation. `angular.json` must be UTF-8 **without BOM**; Transloco keys-manager config is `transloco.config.cjs`.

## Browser routes checked (this pass)

| Route | AR | EN | Notes |
|-------|----|----|-------|
| `/` | Yes | Yes | Playwright Journey A + page-load + viewport |
| `/schools` | Yes | Yes | Search, filters, pagination |
| `/schools/cairo-international-school` | Yes | — | Profile loads |
| `/contact` | Yes | Yes | Form submit + viewport (form usable) |
| `/faq`, `/about`, `/auth/*` | Yes | Yes | Page-load matrix |
| Parent / School / Admin / Support | Yes | Yes | Journeys B–E + authenticated page-load matrices |

## Remaining issues

1. Viewport matrix incomplete for 1024 / 768 across AR+EN for all journeys (390 and 1440 covered by Playwright).
2. Contact page document-level horizontal overflow on some viewports (form remains usable; tracked separately from strict overflow checks).
3. Production bundle budget warnings — non-blocking.
4. Sass `@import` deprecation warnings — non-blocking.

## Automated browser E2E

See [e2e-playwright.md](./e2e-playwright.md). **112 Playwright tests** in `tests/Schoolera.E2E` (Journeys A–E strengthened, page-load matrices, mutations, AR/EN, viewport spot-checks).

Local verification (2026-07-27): `dotnet test tests/Schoolera.E2E` → **112 passed, 0 failed** (single-origin `http://127.0.0.1:5085`).

CI: configure `E2E_SEED_PASSWORD` and branch protection per [github-ci-setup.md](./github-ci-setup.md).

## Sign-off

| Field | Value |
|-------|-------|
| Tester | Auto + local toolchain |
| Date | 2026-07-27 |
| Environment (DB / Node / .NET) | SQL Server local; Node **24.18.0** (portable `.tools/`); .NET **10.0.9** |
| Backend tests | **Pass** (310/310) |
| Playwright E2E | **Pass** (112/112 local) |
| Angular build / Vitest | **Pass** (build to wwwroot; Vitest per CI workflow) |
| Localization | **Pass** (parity + find) |
| Journeys A–E | **Pass** (Playwright browser automation) |
| CSRF + fallback | **Pass** (HTTP integration + SPA implicit in E2E) |
| Localization spot-check | **Pass** (AR RTL / EN LTR in Playwright) |
| CI E2E gate | **Pending** — add `E2E_SEED_PASSWORD` secret + enable `e2e / playwright` required check |
| Known issues | See Remaining issues |
| Ready for Phase 1 release? | **PARTIALLY READY** — CI E2E gate + 1024/768 viewport matrix still open |
