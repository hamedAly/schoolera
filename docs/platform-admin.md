# Platform Admin foundation (Phase 1)

## Purpose

Operational console for **PlatformAdmin** users to review school onboarding, manage school publication status, manage basic user account status, maintain taxonomies, and inspect important admin audit events.

This module reuses existing Authentication, Onboarding, Catalog, and Taxonomy APIs. It does **not** introduce JWT, a second admin role model, or a generic permissions designer.

## Integrations and notifications (Prompt 10)

Platform Admin manages **database-backed** integration configurations and notification
templates. See [platform-integrations.md](./platform-integrations.md).

| Area | Routes |
|------|--------|
| Integrations | `/api/admin/integrations` |
| Notification templates | `/api/admin/notification-templates` |
| Outbox ops summary | `/api/admin/notifications/ops/summary` |

Provider credentials live in `SettingsJson` (**Phase 1 plaintext** — documented limitation).
List/detail APIs return masked settings only. Angular Admin pages: Integrations,
Notification templates, Notifications ops.

## Authorization

All `/api/admin/**` endpoints require the existing policy:

`SchooleraPolicies.PlatformAdminOnly`

| Caller | Expected result |
|--------|-----------------|
| Anonymous | `401` JSON `Result<T>` |
| Authenticated non-PlatformAdmin (Parent, SchoolOwner, SchoolAdmin, SupportAgent) | `403` |
| PlatformAdmin | Allowed |

Frontend `/admin` routes also use `authGuard` + `roleGuard` with `PlatformAdmin`. UI guards are not sufficient alone — APIs enforce the policy.

## Development PlatformAdmin seed

Preserved from Authentication seed (`AuthDataSeeder`). **No second admin seed was added.**

| Setting | Value |
|---------|--------|
| Email | `admin@schoolera.local` (`Auth:SeedUsers:PlatformAdmin:Email`) |
| Password | `Auth:SeedUsers:DefaultPassword` (Development `appsettings.Development.json` / User Secrets) |
| When created | Only when `Database:SeedData=true` **and** `DefaultPassword` is non-empty |
| Missing password | Seed users are skipped; warning logged (password never printed) |
| Idempotency | Existing user by email is not recreated; role is ensured |

Production must bootstrap PlatformAdmin through a secure, documented process (User Secrets / environment configuration). Do not commit production passwords.

See also `docs/authentication.md` (Development seed table).

## API surface

### Dashboard

`GET /api/admin/dashboard`

Returns **real** operational counts only:

- Schools by status (total, published, unpublished, suspended, draft)
- Onboarding by status (submitted/pending, under review, changes requested)
- Active parents / school owners / school admins
- Total users
- Recent audit events (latest 8)

Admissions monitoring is **implemented** (read-only):

- `admissionsAvailable = true`
- Real counts: `admissionApplicationsCount` (total), `admissionDraftCount`, `admissionSubmittedCount`, `admissionUnderReviewCount`, `admissionAcceptedCount`, `admissionRejectedCount`, `admissionCancelledCount`, `admissionApplicationsTodayCount`, `admissionPendingSchoolReviewCount`

Do not treat a missing admissions module as zero applications when `admissionsAvailable` is false (legacy); when true, counts are populated from `GetAdminDashboardMetricsAsync`.

### Admission applications (read-only monitoring)

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/admin/admission-applications` | Search, schoolId, cityId, status, branchId, gradeId, academicYearId, dateFrom, dateTo, sort, pagination |
| GET | `/api/admin/admission-applications/{applicationId}` | Detail incl. `schoolNotes`; masked identity; attachment metadata; full timeline; no `storageKey`/identity hash |
| GET | `/api/admin/admission-applications/export` | CSV stream (`AdminAdmissionApplicationsExportController`); max 5000 rows; UTF-8 BOM; formula-injection safe |

**Not in this phase:** admin `start-review` / `accept` / `reject`, admin attachment download. See [admission-applications.md](./admission-applications.md).

### Schools

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/admin/schools` | Search, status filter, pagination |
| GET | `/api/admin/schools/{schoolId}` | Detail including owner email when present |
| POST | `/api/admin/schools/{schoolId}/status` | CSRF; body `{ status }` (`Draft` / `Published` / `Unpublished` / `Suspended`) |

### Users

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/admin/users` | Search, role, accountStatus, pagination |
| POST | `/api/admin/users/{userId}/status` | CSRF; body `{ accountStatus }` (`Active` / `Suspended` only) |

Rules:

- PlatformAdmin cannot change **their own** account status (`admin.cannotModifySelf` → `403`)
- No admin user creation, password reset, or impersonation in Phase 1

### Audit

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/admin/audit` | Optional `action`, `entityType`, pagination |

Append-only entity: `AdminAuditEvent` (table `AdminAuditEvents`).

Recorded actions include:

- `school.status_changed`
- `user.status_changed`
- `onboarding.approved`
- `onboarding.rejected`

### Existing admin APIs (reused, not duplicated)

- `/api/admin/school-onboarding/**` — review workflow
- `/api/admin/taxonomies/**` — taxonomy CRUD / deactivate for countries, governorates, cities, districts, curricula, stages, grades, facilities, academic years

### CMS and contact requests

- `/api/admin/cms/pages/**` — bilingual static-page list/edit and publish/unpublish/archive workflow
- `/api/admin/cms/faq/**` — bilingual FAQ category/item management, publication, and reorder
- `/api/admin/cms/home/**` — structured bilingual homepage edit and publish/unpublish workflow
- `/api/admin/contact-requests/**` — filtered monitoring, detail, and New/InReview/Resolved/Closed status workflow

CMS and contact mutations require CSRF and append admin audit events. Contact details contain PII and remain PlatformAdmin-only. See [cms-and-contact.md](./cms-and-contact.md) for the complete endpoint and privacy rules.

## Stable error codes

| Code | HTTP |
|------|------|
| `admin.schoolNotFound` | 404 |
| `admin.userNotFound` | 404 |
| `admin.invalidSchoolStatus` | 400 |
| `admin.invalidAccountStatus` | 400 |
| `admin.cannotModifySelf` | 403 |
| `admin.forbidden` | 403 |

Angular must branch on `errorCodes`, never on localized `errors` text.

## Angular SPA

Route: `/admin` (lazy `admin.routes.ts`)

Flow:

`Component → AdminPlatformApi → NSwag Client → API`

Layout mirrors the school portal (sidebar nav, language switcher, logout).

Pages:

- Dashboard (incl. admission counts when `admissionsAvailable`)
- Onboarding list + detail (consume existing onboarding admin APIs)
- **Admission applications** list + read-only detail + CSV export
- Schools list + detail / status
- Users (status only)
- Taxonomies (cities + curricula create/deactivate; list via public taxonomy reads)
- Content pages (`/admin/cms/pages`, `/new`, `/:pageId`)
- FAQ management (`/admin/cms/faq`)
- Homepage content (`/admin/cms/home`)
- Contact requests (`/admin/contact-requests`, `/:requestId`)
- Audit list

The admin sidebar includes Content, FAQ, Homepage, and Contact Requests navigation entries.

Localization: Transloco scope `admin` (`public/i18n/admin/{ar,en}.json`). Arabic default/RTL preserved via `DocumentLanguageService`.

## NSwag note

Adding `/api/admin/schools` and `/api/admin/dashboard` shifted some generated method names:

- Public catalog list: `schoolsGET4`
- School portal accessible schools: `schoolsGET3`
- School portal dashboard: `dashboard2`
- Admin schools list: `schoolsGET`
- Admin dashboard: `dashboard`

Feature services were updated accordingly. Never hand-edit `SwaggerClient.service.ts`.

## Migration

`20260716175554_AddAdminAuditEvents` — creates `AdminAuditEvents`.

## Out of scope (Phase 1)

Permissions designer, role editor, admin user creation, password reset by admin, impersonation, bulk email, **admin admission force actions** (accept/reject/start-review), **admin admission attachment download**, payments, CRM, analytics beyond real counts, notifications center, production deployment.

Cross-school admission **monitoring** (list, detail, CSV export) is implemented — see Admission applications above.
