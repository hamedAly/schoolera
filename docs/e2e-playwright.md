# Playwright .NET E2E

Browser end-to-end regression for Schoolera Phase 1 using **Playwright for .NET** + **xUnit** (`tests/Schoolera.E2E`).

API/unit tests stay in `tests/Schoolera.Tests`. This project drives a real browser against a running API + SPA.

## Prerequisites

- .NET SDK 10.x
- Node 24.18.0 (for Angular when using split Dev hosting)
- SQL Server with Development migrate/seed
- Chromium browsers installed for Playwright

```powershell
dotnet build tests/Schoolera.E2E/Schoolera.E2E.csproj
powershell -ExecutionPolicy Bypass -File tests/Schoolera.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

On CI (Ubuntu), the workflow uses `pwsh ... install chromium --with-deps`.

## Environment variables

| Variable | Required | Example |
|----------|----------|---------|
| `E2E_BASE_URL` | Yes (recommended) | `http://localhost:5100` (local) or `http://127.0.0.1:5085` (CI / hosted SPA) |
| `E2E_SEED_PASSWORD` | **Yes** | Same as `Auth:SeedUsers:DefaultPassword` |
| `HEADED` | No | `1` to show the browser |

Never commit passwords or `tests/Schoolera.E2E/.auth/*.json` (gitignored storage-state cookies).

## Local run (split Dev — recommended)

```powershell
# Terminal 1 — API
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http

# Terminal 2 — Angular (proxy → :5085)
cd src/Schoolera.Api/Schoolera-SPA
npm start -- --port 5100

# Terminal 3 — E2E
$env:E2E_BASE_URL = "http://localhost:5100"
$env:E2E_SEED_PASSWORD = "<seed-password>"
dotnet test tests/Schoolera.E2E/Schoolera.E2E.csproj --settings tests/Schoolera.E2E/.runsettings
```

## Local run (single origin)

```powershell
cd src/Schoolera.Api/Schoolera-SPA
npm run build
cd ../../..
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http

$env:E2E_BASE_URL = "http://127.0.0.1:5085"
$env:E2E_SEED_PASSWORD = "<seed-password>"
dotnet test tests/Schoolera.E2E/Schoolera.E2E.csproj --settings tests/Schoolera.E2E/.runsettings
```

## What is covered

Verified locally: **112 passed** (2026-07-27).

### Journeys A–E

| Journey | Covered in Playwright |
|---------|------------------------|
| **A Visitor** | Home, schools search + filters UI, published profile (fees/gallery sections), contact submit, AR/EN switch |
| **B Parent** | Dashboard, ≥2 seeded children + masked identity, child edit, applications list/detail |
| **C School owner** | Onboarding status, portal overview, catalog sections (branches/stages/fees/gallery) |
| **D Admission review** | Applications list → detail + status/actions (`data-testid`); parent detail hides internal notes |
| **E Platform Admin** | Core admin pages incl. onboarding/applications; CNT-DEMO contacts; school detail when linked |

### Page-load matrices

- Public (+ `/sla`) and auth shells
- Parent static routes
- School portal (16 sections) + onboarding wizard/status
- Admin list/create shells
- Support tickets list

### Mutations + cross-cutting

- Contact validation/submit, parent child create, school profile save, admin taxonomy create, CMS/integration shells, support ticket open
- Auth guards (parent/admin/school/support) + wrong-role denial
- AR/EN `lang`/`dir`; viewport spot-checks (390×844, 1440×900)

### Intentionally not in Playwright (kept elsewhere / deeper follow-up)

- CSRF negative API cases → `Schoolera.Tests`
- Full admission Accept/Reject mutation + parent timeline end-to-end after status change (actions asserted when seed allows; mutation left for seed-safe follow-up)
- Full parent draft→submit application wizard with attachments
- Fresh school-owner register/verify + admin onboarding approve
- Viewport 1024/768 full matrix
- External provider webhooks (Meta/Twilio)

## CI (GitHub Actions)

Workflow: [`.github/workflows/e2e.yml`](../.github/workflows/e2e.yml)

Companion unit gate: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

**One-time setup:** [github-ci-setup.md](./github-ci-setup.md) (secret `E2E_SEED_PASSWORD`, required PR checks).

### Local helper script

```powershell
# API must be running on :5085 (or use split Dev with -BaseUrl)
$env:E2E_SEED_PASSWORD = "<seed-password>"
.\scripts\e2e-local.ps1 -SkipBuild   # after SPA build + API start
```

### Making E2E a required PR check

1. Complete [github-ci-setup.md](./github-ci-setup.md).
2. Confirm green `e2e / playwright` on the default branch.
3. Enable branch protection → require `e2e / playwright` (and optionally `ci / unit`).

Branch protection is a GitHub setting; it cannot be encoded only in the repo.

## Design notes

- Authenticated tests reuse Playwright **storage state** under `.auth/` (created once per collection via seed UI login).
- Prefer role / label / `data-testid` locators; avoid asserting full translated copy.
- CSRF is exercised implicitly through the SPA; explicit CSRF negatives remain in `Schoolera.Tests`.
