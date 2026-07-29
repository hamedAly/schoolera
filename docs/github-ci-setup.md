# GitHub CI setup for Playwright E2E

One-time repository configuration. These steps cannot be committed as code.

## 1. Add repository secret

1. Open the repo on GitHub → **Settings** → **Secrets and variables** → **Actions**.
2. **New repository secret**
   - **Name:** `E2E_SEED_PASSWORD`
   - **Value:** a dedicated CI password (e.g. `Schoolera@E2E-CI-1`) — not a production password.
3. The workflow maps this to `Auth__SeedUsers__DefaultPassword` for API seeding in CI.

Local Development may use a different password via user secrets; CI only needs the secret above.

## 2. Verify workflows

After pushing [`.github/workflows/e2e.yml`](../.github/workflows/e2e.yml) and [`.github/workflows/ci.yml`](../.github/workflows/ci.yml):

| Workflow | Job name | Status check name (typical) |
|----------|----------|-----------------------------|
| `e2e` | `playwright` | `e2e / playwright` |
| `ci` | `unit` | `ci / unit` |

Trigger a PR or push to `Phase1` / `main` and confirm both jobs pass.

### Manual workflow trigger

Push to a branch and open a PR, or use **Actions** → **e2e** → **Run workflow** if `workflow_dispatch` is enabled.

### If `e2e` fails

1. Download **api-log** and **e2e-test-results** artifacts from the failed run.
2. Common causes:
   - Missing `E2E_SEED_PASSWORD` secret
   - SQL Server service not ready (re-run)
   - Angular build failure (check Node 24.18.0 in workflow)
   - Playwright browser install (check `install chromium --with-deps` step)

## 3. Required PR checks (branch protection)

1. **Settings** → **Branches** → **Add branch protection rule** (or edit existing).
2. **Branch name pattern:** `Phase1` (repeat for `main` if needed).
3. Enable **Require status checks to pass before merging**.
4. Search and select:
   - `e2e / playwright` **(required)**
   - `ci / unit` **(recommended)**
5. Save changes.

Exact check names appear in the **Checks** tab of a PR after the first workflow run.

## 4. Team checklist

- [ ] `E2E_SEED_PASSWORD` secret configured
- [ ] First green `e2e / playwright` on default branch
- [ ] First green `ci / unit` on default branch
- [ ] Branch protection requires `e2e / playwright`
- [ ] Developers can run locally via [e2e-playwright.md](./e2e-playwright.md) or `scripts/e2e-local.ps1`

See also: [e2e-playwright.md](./e2e-playwright.md)
