# School Portal

Audit date: 2026-07-16  
Feature: Approved School Portal Management Experience

This document describes the authenticated School Portal for managing schools that already exist after onboarding approval (or another authorized creation path). It does **not** recreate onboarding, publication, contracts, or payments. **Admission application review** for the school is in scope (list, detail, start-review, accept/reject, attachment download); Platform Admin cross-school monitoring is documented in [platform-admin.md](./platform-admin.md).

---

## 1. Architecture

```
Angular School Portal (feature)
  → SchoolPortalApi (feature facade)
  → NSwag Client (SwaggerClient.service.ts)
  → ASP.NET Core /api/school-portal/**
  → MediatR handlers (Schoolera.Application.SchoolPortal)
  → ISchoolPortalRepository / IFileStorage / Identity
  → SQL Server (Schools catalog + SchoolTeamMembers + SchoolAdditionalServices)
```

Production calls remain same-origin relative `/api`. Development uses the Angular proxy.

Multipart logo/cover/gallery uploads may use a narrowly scoped `HttpClient` call inside `SchoolPortalApi` solely for upload progress events. Accept-Language and CSRF remain global.

---

## 2. Ownership and membership

### Ownership (authoritative)

- `School.OwnerUserId` is the only ownership source.
- There is no duplicate Owner row in `SchoolTeamMembers`.
- The Team API/UI still displays the Owner as a synthetic team row (`MembershipId = null`, `IsOwner = true`).

### School-scoped membership

Entity: `SchoolTeamMember`

| Field | Notes |
|-------|--------|
| Id | Guid |
| SchoolId | FK → Schools (Restrict) |
| UserId | Identity user |
| Role | `SchoolAdmin`, `AdmissionOfficer`, `FinanceOfficer`, `ContentModerator` |
| BranchScopeMode | `AllBranches` or `SelectedBranches` (officers only) |
| BranchAssignments | Join rows in `SchoolTeamMemberBranches` when selected |
| IsActive | Soft membership |
| CreatedAtUtc / CreatedByUserId / UpdatedAtUtc / DeactivatedAtUtc | Audit |

Unique filtered index: one **active** membership per `(SchoolId, UserId)`.

Full role/permission matrix: [school-team-roles.md](./school-team-roles.md).

### Global roles vs school access

| Layer | Rule |
|-------|------|
| Global Identity role | `SchoolOwner`, `SchoolAdmin`, `AdmissionOfficer`, `FinanceOfficer`, or `ContentModerator` for policy `SchoolPortal` |
| School access | Owner via `OwnerUserId` **or** active `SchoolTeamMember` for that school |
| Frontend guards | UX only — not a security boundary |

Missing school or no membership → non-enumerating `schoolPortal.schoolNotFound` (HTTP 404).

---

## 3. Permission matrix (summary)

| Capability | Owner | SchoolAdmin | AdmissionOfficer | FinanceOfficer | ContentModerator |
|------------|-------|-------------|------------------|----------------|------------------|
| Team manage / ownership transfer | Yes | No | No | No | No |
| Catalog (profile, branches, offerings) | Yes | Yes | No | No | Profile/content only |
| Admissions review / export / attachments | Yes | Yes | Yes (branch-scoped) | No | No |
| Fees | Yes | Yes | No | Yes (branch-scoped) | No |
| Public content / gallery / facilities / services | Yes | Yes | No | No | Yes |

Suspended schools remain non-editable (`schoolPortal.notEditable`). Branch denials return `schoolPortal.branchOutOfScope` (403).

---

## 4. School status editability matrix

| Status | Portal content editable | Public catalog visible |
|--------|-------------------------|------------------------|
| Draft | Yes | No (existing public rules) |
| Unpublished | Yes | No |
| Published | Yes | Yes (existing public rules) |
| Suspended | No (`schoolPortal.notEditable`) | Per existing public rules |

Portal DTOs never accept or mutate `Status`, `OwnerUserId`, approval fields, or slug (slug is read-only).

---

## 5. Contract Manager extension point

Do **not** add a global Identity role or workflow for Contract Manager in this feature.

Phase 1 school-scoped roles are fixed in [school-team-roles.md](./school-team-roles.md). Do not invent a permission designer or custom-role UI.

---

## 6. API endpoints

Base: `/api/school-portal`  
Auth: cookie + policy `SchoolPortal`  
CSRF: required on all state-changing endpoints (`ValidateAntiForgeryToken`)

| Method | Path |
|--------|------|
| GET | `/schools` |
| GET | `/schools/{schoolId}/dashboard` |
| GET/PUT | `/schools/{schoolId}/profile` |
| GET | `/schools/{schoolId}/branches` |
| GET | `/schools/{schoolId}/branches/{branchId}` |
| POST | `/schools/{schoolId}/branches` |
| PUT | `/schools/{schoolId}/branches/{branchId}` |
| POST | `/schools/{schoolId}/branches/{branchId}/activate` |
| POST | `/schools/{schoolId}/branches/{branchId}/deactivate` |
| GET | `/schools/{schoolId}/offerings?branchId=` |
| POST | `/schools/{schoolId}/offerings` |
| PUT | `/schools/{schoolId}/offerings/{offeringId}` |
| POST | `/schools/{schoolId}/offerings/{offeringId}/activate` |
| POST | `/schools/{schoolId}/offerings/{offeringId}/deactivate` |
| GET | `/schools/{schoolId}/tuition-fees?branchId=` |
| GET | `/schools/{schoolId}/tuition-fees/{feeId}` |
| POST | `/schools/{schoolId}/tuition-fees` |
| PUT | `/schools/{schoolId}/tuition-fees/{feeId}` |
| POST | `/schools/{schoolId}/tuition-fees/{feeId}/activate` |
| POST | `/schools/{schoolId}/tuition-fees/{feeId}/deactivate` |
| GET/PUT | `/schools/{schoolId}/facilities` |
| GET | `/schools/{schoolId}/services` |
| POST | `/schools/{schoolId}/services` |
| PUT | `/schools/{schoolId}/services/{serviceId}` |
| POST | `/schools/{schoolId}/services/{serviceId}/activate` |
| POST | `/schools/{schoolId}/services/{serviceId}/deactivate` |
| PUT | `/schools/{schoolId}/services/order` |
| GET | `/schools/{schoolId}/team` |
| POST | `/schools/{schoolId}/team/members` |
| PUT | `/schools/{schoolId}/team/members/{membershipId}` |
| POST | `/schools/{schoolId}/team/transfer-ownership` |
| POST | `/schools/{schoolId}/team/school-admins` |
| DELETE | `/schools/{schoolId}/team/school-admins/{membershipId}` |
| GET | `/schools/{schoolId}/media` |
| POST/DELETE | `/schools/{schoolId}/media/logo` |
| POST/DELETE | `/schools/{schoolId}/media/cover` |
| POST | `/schools/{schoolId}/media/gallery` |
| PUT | `/schools/{schoolId}/media/gallery/{imageId}` |
| PUT | `/schools/{schoolId}/media/gallery/order` |
| DELETE | `/schools/{schoolId}/media/gallery/{imageId}` |
| GET | `/schools/{schoolId}/applications` |
| GET | `/schools/{schoolId}/applications/{applicationId}` |
| POST | `/schools/{schoolId}/applications/{applicationId}/start-review` |
| POST | `/schools/{schoolId}/applications/{applicationId}/accept` |
| POST | `/schools/{schoolId}/applications/{applicationId}/reject` |
| GET | `/schools/{schoolId}/applications/{applicationId}/attachments/{attachmentId}/download` |

### Admission requirements (school configuration)

| Method | Path |
|--------|------|
| GET | `/schools/{schoolId}/admission-requirements` |
| GET | `/schools/{schoolId}/admission-requirements/{requirementId}` |
| POST | `/schools/{schoolId}/admission-requirements` |
| PUT | `/schools/{schoolId}/admission-requirements/{requirementId}` |
| POST | `/schools/{schoolId}/admission-requirements/reorder` |
| POST | `/schools/{schoolId}/admission-requirements/{requirementId}/publish` |
| POST | `/schools/{schoolId}/admission-requirements/{requirementId}/unpublish` |
| POST | `/schools/{schoolId}/admission-requirements/{requirementId}/deactivate` |

See [admission-requirements.md](./admission-requirements.md) for precedence rules and parent snapshot behavior.

Admission review uses `ISchoolPortalAccess` per `{schoolId}` (same membership rules as other portal endpoints). See [admission-applications.md](./admission-applications.md) for transitions, error codes, and Parent/Admin surfaces.

Stable error codes use the `schoolPortal.*` prefix (see `SchoolPortalErrorCodes`). Admission review also uses `admission.review.*`.

---

## 7. Angular routes

Onboarding routes remain first:

| Path | Purpose |
|------|---------|
| `/school/onboarding` | Owner onboarding wizard |
| `/school/onboarding/status` | Onboarding status |
| `/school` | Portal entry / school selection |
| `/school/:schoolId/overview` | Dashboard |
| `/school/:schoolId/profile` | Profile + logo/cover |
| `/school/:schoolId/branches` | Branches |
| `/school/:schoolId/stages` | Stage/grade offerings |
| `/school/:schoolId/fees` | Tuition fees |
| `/school/:schoolId/facilities` | Facility assignment |
| `/school/:schoolId/gallery` | Gallery media |
| `/school/:schoolId/services` | Additional services |
| `/school/:schoolId/team` | Team |
| `/school/:schoolId/applications` | Admission applications list |
| `/school/:schoolId/applications/:applicationId` | Admission application review detail |
| `/school/:schoolId/admission-requirements` | Admission requirements list/create/publish |

Dashboard (`/school/:schoolId/overview`): `admissionsAvailable = true` with real counts (submitted, underReview, accepted, rejected, totalActive) and `recentSubmittedApplications`.

Entry behavior:

- No accessible school → SchoolOwner redirected toward onboarding/status; others unauthorized.
- One school → redirect to `/school/{id}/overview`.
- Multiple schools → lightweight selector.

Layout: dedicated `SchoolPortalLayout` (not the public header/footer).

Transloco scope: `portal` (`public/i18n/portal/{ar,en}.json`).

---

## 8. Media

### Contexts

| Context | Source of truth | Storage category |
|---------|-----------------|------------------|
| Logo | `School.LogoUrl` | `school-portal/logos` |
| Cover | `School.CoverUrl` | `school-portal/covers` |
| Gallery | `SchoolImage` rows | `school-portal/gallery` |

Public URL pattern: `/uploads/{category}/{safeFileName}` under `FileStorage:StorageRoot` (`App_Data/uploads`).

Private onboarding documents are never used for portal media.

### Configuration (`SchoolPortalMedia`)

| Setting | Default |
|---------|---------|
| MaxLogoBytes | 2 MiB |
| MaxCoverBytes | 5 MiB |
| MaxGalleryImageBytes | 5 MiB |
| MaxGeneralGalleryImages | 20 |
| MaxImagesPerStage | 5 |

Allowed types (via public `FileStorage` options): `.jpg`, `.jpeg`, `.png`, `.webp` with matching content types and magic-byte checks. SVG is not accepted.

### Replacement / deletion

1. Validate and store the new file.
2. Update the database.
3. On DB failure, delete the newly stored file.
4. After success, delete the previous managed URL only if it is inside the configured upload root.
5. Never delete seed assets (`/assets/...`), external URLs, or paths outside the upload root.

### Orphan cleanup

`ISchoolPortalMediaCleanup` / `SchoolPortalMediaOrphanCleanup`:

- Scans only `school-portal/logos|covers|gallery`.
- Deletes files not referenced by `School.LogoUrl`, `School.CoverUrl`, or `SchoolImage.ImageUrl`.
- Supports dry-run.
- Not exposed as a public API (invoked from tests/ops tooling).

---

## 9. Branch, offering, fee, facility, and service rules

### Branches

- City/District consistency required.
- Only one main branch; setting a new main clears the previous.
- At least one active main branch must remain when business rules require it.
- Referenced branches are deactivated, not physically deleted.

### Offerings

Reuse `SchoolStageOffering` / `SchoolGradeOffering`.

- Branch must belong to the school.
- Grades must belong to the selected stage.
- Duplicate active branch/stage/gender offerings rejected.
- Activate/deactivate instead of hard delete.

### Tuition fees

Reuse `TuitionFee` and related informational display entities. See [fee-display-policy.md](./fee-display-policy.md).

- Duplicate active fee definitions rejected (unique filtered index including category).
- No invoicing, payment collection, or accounting.
- Installment rows and published discounts are informational display only.
- Egypt fee-visibility Product default remains **unresolved**; `Schools.FeeVisibilityPolicy` is nullable.

### Facilities

Reuse `SchoolFacility` + Facility taxonomy.

- PUT replaces the selected facility-ID set transactionally.
- Portal users cannot create/edit taxonomy values.

### Additional services

Entity: `SchoolAdditionalService` (bilingual name/description, optional `IconKey`, sort order, active flag).

No pricing, booking, or HTML/executable icon markup.

---

## 10. Team management limitations

- Owner links members by **exact email** of an existing eligible user (no invitation delivery).
- Supported membership roles: SchoolAdmin, AdmissionOfficer, FinanceOfficer, ContentModerator.
- Branch scope only for AdmissionOfficer and FinanceOfficer.
- Parent / PlatformAdmin / SupportAgent accounts are not linked as school staff.
- Deactivation removes school membership only; Identity user is retained.
- Owner cannot be deactivated via membership APIs; ownership transfer is explicit (`transfer-ownership`).
- See [school-team-roles.md](./school-team-roles.md).

---

## 11. Migration and seed

Migrations:

- `20260716161426_AddSchoolPortal` — initial portal tables
- `20260717022747_AddSchoolTeamRolesAndBranchScope` — branch scope + join table

Development seed (idempotent): Owner, SchoolAdmin, AdmissionOfficer, FinanceOfficer, ContentModerator users and demo-school memberships (password from `Auth:SeedUsers:DefaultPassword`).

---

## 12. Known limitations

- No email/SMS/WhatsApp invitation delivery.
- No custom roles or permission designer.
- No publication/review workflow from the portal.
- No Contract Manager role/UI.
- No contracts, payments, CRM, analytics beyond dashboard counts, maps, or CMS.
- No parent/school messaging or admission status notifications (history only; see [admission-applications.md](./admission-applications.md)).
- Slug is read-only (no public URL redirect system).
- Gallery captions on upload are applied via a follow-up update endpoint when needed.
- Angular CLI may require Node ≥ 24.15.0 on this machine; verify with `node --version`.
