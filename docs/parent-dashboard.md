# Parent dashboard, profile, and child profiles

## Purpose

Authenticated **Parent** users manage their profile fields and child profiles, view a dashboard with real admission counts, and complete Parent-facing admission applications. See [admission-applications.md](./admission-applications.md) for the Angular application experience and backend APIs. School messaging and payments are **not** implemented.

## Authorization

Policy: `SchooleraPolicies.ParentOnly` (`Parent` role).

| Caller | Result |
|--------|--------|
| Anonymous | `401` |
| Parent (active) | Allowed |
| SchoolOwner / SchoolAdmin / PlatformAdmin / SupportAgent | `403` |

Frontend `/parent/**` uses `authGuard` + `roleGuard`; APIs enforce the policy independently. Suspended accounts follow existing Identity cookie/account-status behavior.

## ParentProfile

One-to-one with the Parent `ApplicationUser` (`UserId` unique).

**Not duplicated from Identity:** `FirstName`, `LastName`, `PhoneNumber`, `PreferredLanguage`, `Email` remain on `ApplicationUser` and are updated through `IParentAccountService` on profile save.

**ParentProfile columns:** `AlternatePhone`, `AddressLine`, `CityId`, `DistrictId`, `PreferredContactMethod`, timestamps.

Profile row is created on first successful `PUT /api/parent/profile`. `GET` returns Identity fields with empty extension fields when no row exists yet (`Id = Guid.Empty`, `IsComplete = false`).

## ChildProfile

Owned by `ParentUserId` (denormalized) and `ParentProfileId`.

Fields: full name, identity type, protected identity ciphertext, identity lookup hash, identity last four, birth date, gender (`ChildGender`), current grade, `CurrentSchoolName`, `PreferredStudyLanguage` (`ChildStudyLanguage`), skills/hobbies/strengths/improvement areas, special-needs flags/notes, `HealthNotes` (parent detail only), `IsActive`, timestamps.

Age is computed from `BirthDate` and never persisted.

**List vs detail:** list returns `CurrentSchoolName` / `PreferredStudyLanguage` when set; never returns `HealthNotes`, `SpecialNeedsNotes`, or free-text skills/hobbies/strengths/improvement areas. Detail returns all extension fields including `HealthNotes`.

### Child Document Vault

Private Parent-owned files per child (`ChildDocument`). Storage keys are never returned. Category: `child-documents/{childId}`.

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/parent/children/{childId}/documents` | Owned list |
| POST | `/api/parent/children/{childId}/documents` | CSRF; multipart `documentType` + `file`; 10 MB |
| PUT | `/api/parent/children/{childId}/documents/{documentId}` | CSRF; replace multipart |
| DELETE | `/api/parent/children/{childId}/documents/{documentId}` | CSRF; hard delete |
| GET | `/api/parent/children/{childId}/documents/{documentId}/download` | Stream; nosniff / no-store |

Unknown/non-owned child or document → `404` `parent.child.notFound` / `parent.child.documentNotFound`. Copy into an admission draft: `POST /api/parent/admission-applications/{id}/attachments/from-vault` — see [admission-applications.md](./admission-applications.md).

### Sensitive identity

- Full identity accepted only on create or explicit replace.
- Normalized (digits only), validated with **generic** length rules (no Egyptian NIN checksum — not documented for Schoolera).
- Lookup: HMAC-SHA256 with `ParentIdentityProtection:HmacKeyBase64` (Development has a dedicated non-production key; Production must use secrets / key ring).
- Storage: ASP.NET Core Data Protection ciphertext (`ProtectedIdentityValue`) + last four for masking.
- Responses return `maskedIdentity` only (`************1234`). Ciphertext and hashes are never returned.
- Never logged, never placed in URLs/localStorage/audit metadata.

### Duplicate scope

**Within-parent uniqueness** on `(ParentUserId, IdentityLookupHash)`.

System-wide uniqueness across parents is **not** enforced — there is no documented legal requirement for platform-wide national-ID uniqueness. Error code: `parent.child.identityAlreadyExists` (does not disclose another Parent’s ownership).

## Ownership

All child reads/writes load by `(ParentUserId == current user, ChildId)`. Unknown and non-owned children share `404` `parent.child.notFound`. Request bodies never accept ParentUserId.

## APIs

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/parent/dashboard` | Real child + application counts; recent activity from admissions |
| GET | `/api/parent/profile` | |
| PUT | `/api/parent/profile` | CSRF |
| GET | `/api/parent/children` | Active owned children |
| GET | `/api/parent/children/{childId}` | |
| POST | `/api/parent/children` | CSRF |
| PUT | `/api/parent/children/{childId}` | CSRF; omit identity to keep existing |
| DELETE | `/api/parent/children/{childId}` | CSRF; soft-deactivate (`IsActive=false`) |
| GET/POST/PUT/DELETE | `/api/parent/children/{childId}/documents/**` | Private Child Document Vault (see above) |

Admission application endpoints: `/api/parent/admission-applications/**` — see [admission-applications.md](./admission-applications.md).

Application detail returns outcome timestamps (`reviewStartedAtUtc`, `acceptedAtUtc`, `rejectedAtUtc`) and `parentVisibleRejectionReason` when the school has rejected with a parent-visible note. Parents never see `schoolNotes` or internal rejection text.

### Dashboard

- `applicationsAvailable = true`
- Real counts: `totalApplications`, `draftApplications`, `submittedApplications`, `underReviewApplications`, `acceptedApplications`, `rejectedApplications`, `cancelledApplications`
- `recentActivityAvailable` / `recentActivities` from parent-visible admission history (up to 5); empty when none
- `profileIncomplete` when city/district missing

### Delete

Phase 1 soft-deactivates when no dependents exist. If **any** admission application references the child, delete returns `parent.child.cannotDeleteReferencedChild` (hard delete remains reserved for later).

## Angular

Routes under `/parent` (ParentLayout): dashboard, profile, children, children/new, children/:id/edit, **applications**, applications/new, applications/:id, applications/:id/edit, applications/:id/success.

Nav: Dashboard, My Profile, My Children, **My Applications**.

`ParentApi` → NSwag `Client` (dashboard, profile, children, child documents, admission applications, attachments). Attachment upload/download use scoped HttpClient for progress/blob (same pattern as school portal media).

Transloco scope `parent`.

Dashboard shows real `applicationsAvailable` counts and parent-visible recent activity. Start Application links to public school search; View All links to `/parent/applications`.

Admission UI details: [admission-applications.md](./admission-applications.md).

## Migration

`AddParentAndChildProfiles` (tables `ParentProfiles`, `ChildProfiles`). Child extensions + vault: `AddChildProfileExtensionsAndDocumentVault` (`ChildDocuments`, child free-text/study-language columns, admission snapshot extension columns, `SourceVaultDocumentId`). Admission tables: see [admission-applications.md](./admission-applications.md).

## Configuration

```json
"ParentChild": { "MinAgeYears": 3, "MaxAgeYears": 20 },
"ParentIdentityProtection": { "HmacKeyBase64": "<base64 32+ bytes>" }
```

School-age range is a product convention for Phase 1 (ages 3–20 inclusive).
