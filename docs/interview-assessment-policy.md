# Interview and assessment policy

School-owned interview/assessment policies define whether parents must complete an interview, assessment, both, or neither. Policies are bilingual (Arabic/English), scoped with the same catalog precedence as admission requirements, and published explicitly from the school portal.

## Precedence (most specific wins)

Exactly **one** published active policy applies per application selection. When multiple published policies match, the highest **specificity score** wins (ties broken by policy `Id`):

| Score | Scope |
|------:|-------|
| 7 | Branch + Grade + Academic year |
| 6 | Branch + Stage + Academic year (no grade) |
| 5 | Grade + Academic year (no branch) |
| 4 | Stage + Academic year (no branch/grade) |
| 3 | Branch + Academic year only |
| 2 | Academic year only |
| 1 | School default (no scope segments) |

Equal specificity at the same `ScopeKey` is blocked at publish (`schoolPortal.interviewAssessmentPolicyConflict`). Scoring uses the shared `AdmissionScope` helper — there is no second resolver.

## Requirement modes

- **NotRequired** — no interview/assessment operational fields; delivery/participants/booking/provider cleared.
- **InterviewOnly** / **AssessmentOnly** / **InterviewAndAssessment** — require delivery mode, participants, duration, booking/lead bounds, bilingual preparation notes, and mode-specific instructions.

## Delivery and Meeting boundary

- **OnSite** / **Hybrid** publish requires a branch on the policy scope with an identifiable address (`AddressLineAr` / `AddressLineEn` / `StreetName`). School-default OnSite (null branch) cannot publish.
- **Online** / **Hybrid** require an active platform `IntegrationType.Meeting` provider code. Phase 1 allows only `Development` / `Simulated` (empty `{}` settings OK). Real Zoom/Teams adapters are out of scope.
- Snapshots never store credentials, `SettingsJson`, or live meeting URLs.

## Parent application flow

1. **Create draft** — when a published applicable policy exists, an immutable one-per-application snapshot is created (`PolicySnapshotCreated`).
2. **No published match** — no snapshot; parent summary is null / not configured.
3. **Update selection (scope change)** — if interview/assessment appointments already exist, fail with `admission.application.policySnapshotScopeChangeBlocked`. Otherwise replace the snapshot (`PolicySnapshotReplaced`).
4. **Backfill** — if no snapshot exists yet (and scope did not change), ensure once like requirements.

## Unresolved product note

Interview → Assessment ordering (when both are required) is **not** resolved in this prompt. Scheduling slots and real meeting adapters are deferred.

## School portal API

Base route: `/api/school-portal/schools/{schoolId}/interview-assessment-policies`

Mutations require CSRF and `SchoolPortal` policy. Permission: `ManageAdmissionRequirements` (same matrix as admission requirements). Published policies must be **unpublished** before structural edits. `PolicyVersion` starts at 1 on create; the first publish keeps version 1; later publish cycles increment.

## Public preview

`GET /api/schools/{slug}/interview-assessment-policy?branchId&educationalStageId&gradeId&academicYearId` returns a localized `SafeInterviewAssessmentPolicySummaryDto` or `null`.

See also: [admission-requirements.md](./admission-requirements.md), [admission-applications.md](./admission-applications.md).
