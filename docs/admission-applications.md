# Admission Applications (backend)

Parent-owned school admission applications. The student is the existing `ChildProfile` (no separate Student entity). Parent APIs, School Portal review APIs, and Platform Admin monitoring APIs share the same domain model and migration. Parent and School Portal Angular pages are implemented (this document).

See also: [parent-dashboard.md](./parent-dashboard.md), [school-portal.md](./school-portal.md), [platform-admin.md](./platform-admin.md), [school-catalog.md](./school-catalog.md), [authentication.md](./authentication.md).

## Domain model

| Entity | Role |
|--------|------|
| `AdmissionApplication` | Parent application for a child at a school/branch/stage/grade/year |
| `AdmissionApplicationAttachment` | Optional private file metadata; opaque `StorageKey` |
| `AdmissionApplicationHistory` | Append-only timeline; parent vs internal visibility |
| `AdmissionApplicationNumberSequence` | Year-scoped counter for application numbers |

**ChildProfile reuse:** applications reference `ChildProfileId` + denormalized `ParentUserId` / `ParentProfileId`. Child identity remains on `ChildProfile` (masked on Parent DTOs). Soft-deactivating a child is blocked when any admission row exists (`parent.child.cannotDeleteReferencedChild`).

### `AdmissionApplication` fields

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `ApplicationNumber` | Unique; format below |
| `ParentUserId`, `ParentProfileId` | Owner |
| `ChildProfileId` | FK → `ChildProfile` |
| `SchoolId`, `SchoolBranchId` | Target school/branch |
| `EducationalStageId`, `GradeId`, `AcademicYearId` | Selection |
| `Status` | Enum (int) |
| `ParentNotes` | Parent-facing (max 2000) |
| `SchoolNotes` | Internal; never on Parent APIs |
| `SubmittedAtUtc`, `ReviewStartedAtUtc`, `AcceptedAtUtc`, `RejectedAtUtc`, `CancelledAtUtc` | Lifecycle timestamps |
| `RejectionReason` | Internal; Parent-visible copy only via history when `ParentVisible` |
| `CancellationReason` | Optional |
| `SubmittedChildFullName`, `SubmittedSchoolNameAr/En`, `SubmittedBranchNameAr/En`, `SubmittedStageNameAr/En`, `SubmittedGradeNameAr/En`, `SubmittedAcademicYearNameAr/En` | Bounded snapshots at submit (max 200 each) |
| `SubmittedChildCurrentSchoolName`, `SubmittedChildPreferredStudyLanguage`, `SubmittedChildSkills/Hobbies/Strengths/ImprovementAreas`, `SubmittedChildHasSpecialNeeds`, `SubmittedChildSpecialNeedsNotes` | Approved school-visible child extensions at submit. **Never** snapshots `HealthNotes`. |
| `CreatedAtUtc`, `UpdatedAtUtc` | |
| `RowVersion` | Concurrency token |

### `AdmissionApplicationAttachment` fields

| Field | Notes |
|-------|--------|
| `Id`, `AdmissionApplicationId` | |
| `AttachmentType` | `SupportingDocument`, `BirthCertificate`, `PreviousSchoolReport`, `Other` — optional; none required in Phase 1 |
| `OriginalFileName`, `ContentType`, `FileSizeBytes` | Client metadata |
| `StorageKey` | Opaque private reference; **never** returned to Angular |
| `SourceVaultDocumentId` | Internal draft idempotency link to a Child Vault document; **never** returned in DTOs. Application stores a separate copied `StorageKey`. |
| `UploadedByUserId`, `CreatedAtUtc` | |

### `AdmissionApplicationHistory` fields

| Field | Notes |
|-------|--------|
| `Id`, `AdmissionApplicationId` | |
| `FromStatus`, `ToStatus` | Nullable from on create |
| `Action` | Stable names (`Created`, `Updated`, `Submitted`, `Cancelled`, `AttachmentUploaded`, `AttachmentRemoved`, `ReviewStarted`, `Accepted`, `Rejected`) |
| `ActorUserId`, `ActorRole` | |
| `ParentVisible` | Parent timeline filter |
| `ParentVisibleNote`, `InternalNote` | Parent APIs expose only parent-visible note |

## Application number

Format: `APP-{UTC year}-{sequence:D6}` (example: `APP-2026-000001`).

`AdmissionApplicationNumberGenerator` locks the dedicated year row in `AdmissionApplicationNumberSequences` with SQL Server hints **`UPDLOCK, ROWLOCK, HOLDLOCK`** inside an EF execution-strategy transaction, increments `LastValue`, and returns the formatted number. Not an in-memory counter.

## Statuses

| Value | Name |
|------:|------|
| 1 | Draft |
| 2 | Submitted |
| 3 | UnderReview |
| 4 | Accepted |
| 5 | Rejected |
| 6 | Cancelled |
| 7 | MissingDocuments |
| 8 | InterviewRequired |
| 9 | AssessmentRequired |
| 10 | WaitingList |
| 11 | Registered |

**Intentionally absent:** `ContractSent`, `Paid` (later prompts).

Extended lifecycle (missing documents, interview/assessment, waiting list, registered): see [admission-lifecycle.md](./admission-lifecycle.md).

## Transitions

| Actor | Transition | Notes |
|-------|------------|--------|
| Parent | Create → Draft | Always starts as Draft |
| Parent | Draft → Submitted | After eligibility re-check + snapshot |
| Parent | Draft → Cancelled | Allowed |
| Parent | Submitted → Cancelled | Only before review (`ReviewStartedAtUtc` is null) |
| School | Submitted → UnderReview | `start-review` (CSRF) |
| School | UnderReview → Accepted | `accept` (CSRF) |
| School | UnderReview → Rejected | `reject` (CSRF; requires `parentVisibleRejectionReason`) |

**Not allowed (school):** Submitted→Accepted/Rejected directly; Draft review; Cancelled review; Accepted↔Rejected; `ContractSent`/`Paid` (statuses absent).

Central policy: `AdmissionTransitionPolicy.TryValidateSchoolTransition` / `TryValidateParentTransition`. Each API surface exercises only its allowed transitions.

## Duplicate active applications

**Active** = statuses Draft, Submitted, UnderReview, Accepted (1–4).

Uniqueness key: **Child + School + Branch + Grade + AcademicYear**.

SQL filtered unique index: `IX_AdmissionApplications_ActiveDuplicate` with filter `[Status] IN (1, 2, 3, 4)`.

**Cancelled** and **Rejected** permit a new application for the same selection. Race conflicts map to `admission.application.duplicateActiveApplication`.

## Admission eligibility

Validated by `AdmissionEligibilityService` on create, update, and submit:

1. Owned, active child (when required)
2. Parent profile exists
3. School is **Published** (not status alone for “open”)
4. Branch belongs to school and is **active**
5. Stage and grade active; grade belongs to stage
6. Academic year **active**
7. Active stage offering with `IsAdmissionOpen` matching stage + child gender
8. Active grade offering for that stage offering
9. Child gender eligible for school `GenderType` and offering `GenderType`

Catalog “admission open” badges/filters are a subset; applying requires the full checks above. See [school-catalog.md](./school-catalog.md).

## Ownership and safe 404

Loads use `GetOwned*` / owned child lookups by current Parent user id. Unknown ids and non-owned applications/attachments share **`404`** with `admission.application.notFound` / `admission.application.attachmentNotFound` (no ownership leak). Request bodies never accept `ParentUserId`.

## Parent API endpoints

Controller: `ParentAdmissionApplicationsController`  
Policy: `SchooleraPolicies.ParentOnly`  
Prefix: `/api/parent/admission-applications`

| Method | Path | CSRF | Notes |
|--------|------|:----:|-------|
| GET | `/` | | Paged list; filters: status, childProfileId, schoolId, academicYearId, search, sort, paging |
| POST | `/` | ✓ | Create draft |
| GET | `/{applicationId}` | | Detail + parent-visible timeline + capabilities |
| PUT | `/{applicationId}` | ✓ | Draft-only selection/notes update (`RowVersion`) |
| POST | `/{applicationId}/submit` | ✓ | Draft → Submitted; returns `SubmitAdmissionOutcomeDto` (`application`, `missingRequirements`). Blocks with `admission.application.requirementsIncomplete` when required snapshots are incomplete. |

Requirement snapshots are created once per draft (see [admission-requirements.md](./admission-requirements.md)). Submit evaluates profile fields and typed document slots against immutable snapshots.
| POST | `/{applicationId}/cancel` | ✓ | Optional reason body |
| POST | `/{applicationId}/attachments` | ✓ | Multipart; draft-only; size limit 10 MB request |
| POST | `/{applicationId}/attachments/from-vault` | ✓ | Body `{ childDocumentId }`; copies vault file into draft (idempotent on `SourceVaultDocumentId`) |
| DELETE | `/{applicationId}/attachments/{attachmentId}` | ✓ | Draft-only |
| GET | `/{applicationId}/attachments/{attachmentId}/download` | | Authenticated stream; not a public URL |

Stable error codes under `admission.application.*` (branch on codes, not localized text).

Parent DTOs never include `SchoolNotes`, `RejectionReason` (raw), or `StorageKey`. Rejection surfaces as `ParentVisibleRejectionReason` on detail (from parent-visible history). Detail also exposes `ReviewStartedAtUtc`, `AcceptedAtUtc`, and `RejectedAtUtc` when set.

## School Portal API endpoints

Controllers: `SchoolPortalAdmissionApplicationsController`, `SchoolPortalAdmissionAttachmentsController` (streaming download only)  
Policy: `SchooleraPolicies.SchoolPortal` + school access via `ISchoolPortalAccess` (membership required; **not** role alone)  
Prefix: `/api/school-portal/schools/{schoolId}/applications`

| Method | Path | CSRF | Notes |
|--------|------|:----:|-------|
| GET | `/` | | Paged list; filters: status, branchId, gradeId, educationalStageId, academicYearId, search, dateFrom, dateTo, sort, pageNumber, pageSize |
| GET | `/{applicationId}` | | Detail + full timeline (incl. internal notes) + `SchoolNotes` + capabilities. After submit, child name / special needs / extension fields prefer submission snapshot (`StudentCurrentSchoolName`, `StudentPreferredStudyLanguage`, skills/hobbies/strengths/improvementAreas). **Never** `HealthNotes`. |
| POST | `/{applicationId}/start-review` | ✓ | Submitted → UnderReview; optional `internalReviewNote`, `rowVersion` |
| POST | `/{applicationId}/accept` | ✓ | UnderReview → Accepted; optional `internalReviewNote`, `rowVersion` |
| POST | `/{applicationId}/reject` | ✓ | UnderReview → Rejected; **required** `parentVisibleRejectionReason`; optional `internalReviewNote`, `rowVersion` |
| GET | `/{applicationId}/attachments/{attachmentId}/download` | | Authenticated stream (`SchoolPortalAdmissionAttachmentsController`) |

### School Portal authorization

| Caller | Result |
|--------|--------|
| Anonymous | `401` |
| Parent / PlatformAdmin / SupportAgent | `403` (policy) |
| SchoolOwner / SchoolAdmin without membership for `{schoolId}` | `404` `schoolPortal.schoolNotFound` |
| SchoolOwner / SchoolAdmin for a different school | `404` `schoolPortal.schoolNotFound` |
| Owner or active `SchoolTeamMember` for `{schoolId}` | Allowed |

Application not found or wrong school → `404` `admission.review.notFound` (non-enumerating).

School DTOs include `SchoolNotes` and internal `RejectionReason`. Parents never see these fields. On reject, `parentVisibleRejectionReason` is stored in parent-visible history for Parent APIs.

## Platform Admin API endpoints (read-only monitoring)

Controllers: `AdminAdmissionApplicationsController`, `AdminAdmissionApplicationsExportController` (CSV stream)  
Policy: `SchooleraPolicies.PlatformAdminOnly`  
Prefix: `/api/admin/admission-applications`

| Method | Path | CSRF | Notes |
|--------|------|:----:|-------|
| GET | `/` | | Paged list; filters: search, schoolId, cityId, status, branchId, gradeId, academicYearId, dateFrom, dateTo, sort, pageNumber, pageSize |
| GET | `/{applicationId}` | | Detail for monitoring: includes `schoolNotes`; masked student identity; attachment metadata only; full timeline with actor; **no** `storageKey` or identity hash |
| GET | `/export` | | CSV download (`AdminAdmissionApplicationsExportController`; not `Result<T>` envelope) |

**Not implemented:** admin `start-review` / `accept` / `reject`, admin attachment download (metadata only on detail).

### Admin CSV export

- Max rows: **5000** (`AdminAdmissionCsvExporter.MaxExportRows`)
- Columns: ApplicationNumber, Status, SchoolName, CityName, BranchName, StudentName, ParentName, GradeName, AcademicYearName, SubmittedAtUtc, ReviewStartedAtUtc, DecisionAtUtc
- UTF-8 BOM; formula-injection prefix for leading `=`, `+`, `-`, `@`, tab, CR
- Excludes identity, `storageKey`, and `schoolNotes`

## School review error codes

Under `admission.review.*` (branch on codes, not localized text):

| Code | Typical HTTP |
|------|----------------|
| `admission.review.notFound` | 404 |
| `admission.review.invalidTransition` | 400 |
| `admission.review.rejectionReasonRequired` | 400 |
| `admission.review.concurrentUpdate` | 409 |
| `admission.review.schoolAccessDenied` | 403 |

Parent application codes remain under `admission.application.*`.

## Attachments and privacy

- Stored via `IPrivateFileStorage` (`LocalPrivateFileStorage`) under a private root **not** served by static files.
- Category path: `admission-applications/{applicationId:N}`.
- Clients receive metadata + authenticated download only; no public `/uploads` URLs for admission files.
- Allowed types/size follow `PrivateFileStorage` options (PDF/JPEG/PNG/WebP allowlist; default max 10 MB).

## Timeline

Parent detail returns only history rows with `ParentVisible == true` (`AdmissionHistoryDto`). Internal notes and non-visible school actions are omitted.

## Dashboard integration

`GET /api/parent/dashboard`:

- `applicationsAvailable = true`
- Real counts: total, draft, submitted, underReview, accepted, rejected, cancelled
- `recentActivityAvailable` / `recentActivities` from parent-visible history (latest 5)

`GET /api/school-portal/schools/{schoolId}/dashboard`:

- `admissionsAvailable = true`
- Real counts: submitted, underReview, accepted, rejected, totalActive
- `recentSubmittedApplications` (latest submitted items)

`GET /api/admin/dashboard`:

- `admissionsAvailable = true`
- Real counts: total (`admissionApplicationsCount`), draft, submitted, underReview, accepted, rejected, cancelled, today (`admissionApplicationsTodayCount`), pending school review (`admissionPendingSchoolReviewCount`)

## Development seed

`AdmissionApplicationSeeder` runs after auth users and catalog/portal seed (`Database:SeedData=true`).

- Parent: `parent@schoolera.local`
- School: published `cairo-international-school`
- Up to three apps on distinct stage/grade slots: **Draft**, **Submitted**, **UnderReview** (UnderReview via seeder-only `StartReview`, not Parent API)
- No attachments
- Idempotent: skips if any application already exists for that parent

## Notifications

No message bus, email, or SMS on status changes. `AdmissionApplicationHistory` is the authoritative audit trail. Parent/school/admin UIs read history; push/email notifications are **pending** a later phase.

## CSRF

State-changing Parent admission actions use **per-action** `[ValidateAntiForgeryToken]` (cookie + `X-XSRF-TOKEN` header). Antiforgery is **not** disabled.

Integration tests (`AdmissionApplicationTests`):

- Missing CSRF header → **400**
- Invalid CSRF header → **400**
- Valid CSRF → accepted (or non-antiforgery business validation)

Earlier PowerShell smoke tests that appeared to return **200** without an explicit CSRF header were using a reused `HttpClient`/`DefaultRequestHeaders` session that still carried `X-XSRF-TOKEN` from a prior authenticated call (for example `/api/auth/me`). That was not evidence that antiforgery was off.

## Migration

`20260716192657_AddAdmissionApplications` — tables `AdmissionApplications`, `AdmissionApplicationAttachments`, `AdmissionApplicationHistory`, `AdmissionApplicationNumberSequences`, plus the filtered unique index.

`AddChildProfileExtensionsAndDocumentVault` — child extension snapshot columns on `AdmissionApplications`, nullable `SourceVaultDocumentId` on attachments, and `ChildDocuments` vault table.

## Known limitations

- No required attachment types
- Private files on local disk only (`LocalPrivateFileStorage`)
- No ContractSent/Paid statuses
- No application fees / payments / contracts
- No email/SMS/push notifications (history only)
- No PlatformAdmin force actions (accept/reject/start-review) or admin attachment download
- Phase 1 terms/consent use static localized copy (no versioned legal Terms backend)
- Parent contact on the application is live profile data, not a frozen snapshot
- Academic years for the Apply form come from `GET /api/taxonomies/academic-years` (active taxonomy years); branch/stage/grade options come from the published school profile offerings filtered to `isAdmissionOpen`
- Public school profile offerings expose `educationalStageId` for Apply selection (additive DTO field)

## Parent Angular experience

Routes (auth + Parent role):

| Path | Purpose |
|------|---------|
| `/parent/applications` | List with status/child/search filters and server pagination |
| `/parent/applications/new?schoolSlug=` | Start flow (hints: branchId, stageId, gradeId, academicYearId, childId) |
| `/parent/applications/:applicationId` | Detail, timeline, cancel when `canCancel` |
| `/parent/applications/:applicationId/edit` | Draft editor when `canEdit` |
| `/parent/applications/:applicationId/success` | Post-submit confirmation with real application number |

Steps: Student → School details → Parent details → Notes/documents → Review/submit.

Draft is created only after child + school + branch + stage + grade + year are selected; then the UI routes to the real application id. Explicit save on step advance; debounced notes autosave. Unsaved-change `CanDeactivate` guard on editor routes.

School Profile Apply CTA (`قدّم الآن` / Apply Now) when `environment.features.admissionsEnabled`, school `isAdmissionOpen`, and at least one admission-open offering with grades + stage id. Anonymous users login with `returnUrl` to the start flow. Non-Parents go to `/unauthorized`.

Add-child detour: `/parent/children/new?returnUrl=...` returns to the application flow and selects the new child when possible.

Capability flags from the API drive edit/submit/cancel/attachment actions. Status badges cover Draft…Cancelled with a safe unknown fallback. Detail shows `reviewStartedAtUtc`, `acceptedAtUtc`, `rejectedAtUtc`, and `parentVisibleRejectionReason` when applicable.

Feature service: `ParentApi` wraps NSwag; components do not inject `Client`.

## School Portal Angular experience

Routes (auth + SchoolOwner/SchoolAdmin + school context):

| Path | Purpose |
|------|---------|
| `/school/:schoolId/applications` | List with status/branch/grade/stage/year/search/date filters and server pagination |
| `/school/:schoolId/applications/:applicationId` | Detail, timeline, start-review / accept / reject when capabilities allow; attachment download |

Nav: **Applications** entry in `SchoolPortalLayout`. Feature service: `SchoolPortalApi` wraps NSwag (`applications`, `applications2`, review POSTs) + HttpClient blob download for attachments.

## Platform Admin Angular experience

Routes (auth + PlatformAdmin):

| Path | Purpose |
|------|---------|
| `/admin/applications` | Cross-school list with filters, pagination, CSV export |
| `/admin/applications/:applicationId` | Read-only monitoring detail (incl. `schoolNotes`; no attachment download) |

Nav: **Applications** entry in `AdminLayout`. Feature service: `AdminPlatformApi` wraps NSwag list/detail + HttpClient CSV export from `/api/admin/admission-applications/export`.
