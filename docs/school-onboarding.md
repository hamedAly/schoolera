# School owner educational institution onboarding

## Overview

SchoolOwners complete a multi-step onboarding application. PlatformAdmins review applications through API endpoints. Approval creates an **unpublished** `School` with a primary `SchoolBranch` linked to the owner. Documents are stored privately and downloaded only through authorized endpoints.

See also: [authentication.md](./authentication.md), [school-catalog.md](./school-catalog.md).

## Aggregate entities

| Entity | Role |
|--------|------|
| `SchoolOnboardingApplication` | Aggregate root (owner draft → review → terminal state) |
| `SchoolOnboardingDocumentType` | Configurable bilingual document types |
| `SchoolOnboardingDocument` | Uploaded file metadata (`IsCurrent` replacement model) |
| `SchoolOnboardingStatusHistory` | Append-only audit of status transitions |

## Status transitions

| From | To | Actor |
|------|----|-------|
| Draft | Submitted | SchoolOwner (`POST .../submit`) |
| Submitted | UnderReview | PlatformAdmin (`.../start-review`) |
| UnderReview | ChangesRequested | PlatformAdmin (`.../request-changes`, owner-visible reason required) |
| UnderReview | Approved | PlatformAdmin (`.../approve`) |
| UnderReview | Rejected | PlatformAdmin (`.../reject`, owner-visible reason required) |
| ChangesRequested | Submitted | SchoolOwner (`POST .../resubmit`) |

Approved and Rejected are terminal for this prompt.

Editable only when status is **Draft** or **ChangesRequested**.

## Draft vs submit validation

- Step PUT endpoints accept incomplete later steps (field-level validation for supplied fields).
- Submit/resubmit run full aggregate validation (organization, representative, school, branch/city-district, required documents, formats).
- Stable error codes under `onboarding.*` (see `OnboardingErrorCodes`).

## Ownership and authorization

| Surface | Policy / role |
|---------|----------------|
| `/api/school-onboarding/**` | `SchoolOwnerOnly` |
| `/api/admin/school-onboarding/**` | `PlatformAdminOnly` |

- Parents and SchoolAdmins cannot own an onboarding application.
- Owners only see their own application/documents (non-enumerating failures where applicable).
- Owner DTOs never include internal admin notes.

## CSRF

All state-changing cookie-authenticated endpoints use `[ValidateAntiForgeryToken]` (`XSRF-TOKEN` / `X-XSRF-TOKEN`), consistent with Phase 1 auth.

## Private document storage

- Interface: `IPrivateFileStorage` / `LocalPrivateFileStorage`
- Root: `App_Data/private/school-onboarding` (not served by static files)
- Opaque stored references only — never physical paths or public URLs
- Downloads stream through authorized controllers with `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, and private/no-store cache headers

### Allowed formats and size

- Extensions: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.webp`
- Content-Types: `application/pdf`, `image/jpeg`, `image/png`, `image/webp`
- Max size: 10 MiB (configurable via `PrivateFileStorage:MaxFileSizeBytes`)
- Validation: extension, content-type, magic-byte signature (including PDF `%PDF-`), non-empty, path traversal protection, cryptographically safe stored names

## Document types (Development seed)

Idempotent by `Code` via `OnboardingSeeder`:

| Code | Required |
|------|----------|
| `organization-registration` | Yes |
| `educational-license` | Yes |
| `tax-document` | No |
| `authorized-representative-letter` | Yes |

## Approval behavior

Approval is transactional and idempotent:

1. Application must be `UnderReview` and fully valid.
2. If `ApprovedSchoolId` already set → return existing state (no duplicates).
3. Otherwise create `School` with `SchoolStatus.Unpublished`, assign `OwnerUserId`, create main `SchoolBranch` from onboarding address/taxonomies.
4. Unique slug via existing `SlugHelper` with collision suffixing (does not overwrite other schools).
5. Mark application Approved and append status history.

**Publication decision:** approval does **not** publish the school publicly. Public catalog continues to require the published status used by existing school APIs.

## SchoolOwner API routes

Prefix: `/api/school-onboarding`

| Method | Route |
|--------|-------|
| GET | `/me` |
| GET | `/document-types` |
| PUT | `/me/organization` |
| PUT | `/me/authorized-representative` |
| PUT | `/me/school-details` |
| PUT | `/me/primary-branch` |
| POST | `/me/documents` (multipart) |
| DELETE | `/me/documents/{documentId}` |
| GET | `/me/documents/{documentId}/download` |
| POST | `/me/submit` |
| POST | `/me/resubmit` |

## PlatformAdmin API routes

Prefix: `/api/admin/school-onboarding`

| Method | Route |
|--------|-------|
| GET | `/` (paged list + status filter) |
| GET | `/{applicationId}` |
| POST | `/{applicationId}/start-review` |
| POST | `/{applicationId}/request-changes` |
| POST | `/{applicationId}/approve` |
| POST | `/{applicationId}/reject` |
| GET | `/{applicationId}/documents/{documentId}/download` |

## Angular

| Route | Access |
|-------|--------|
| `/school/onboarding` | SchoolOwner — multi-step wizard |
| `/school/onboarding/status` | SchoolOwner — status page |

Data access: `SchoolOnboardingApi` wraps NSwag `Client`.

**Upload progress exception:** multipart upload uses a narrowly scoped `HttpClient` call inside `SchoolOnboardingApi` (`reportProgress: true`) because the generated client does not expose progress events. Paths remain relative; CSRF and Accept-Language interceptors still apply. Downloads also use `HttpClient` for blob streaming.

Locked statuses (`Submitted`, `UnderReview`, `Approved`, `Rejected`) redirect the wizard to the status page.

## Migration

`20260716132138_AddSchoolOnboarding`

## Known limitations

- No Angular PlatformAdmin review UI in this prompt (APIs only).
- No map picker, OCR, antivirus, cloud storage, government identity verification, admissions, payments, or CRM.
- Authenticated CSRF/upload smoke flows require a running API + seeded credentials and are not fully automated in unit tests.
- Approval creates unpublished schools; public listing remains separate.
