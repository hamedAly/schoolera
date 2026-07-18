# Child age eligibility rules

School-owned age eligibility rules define the completed calendar months a child must have reached (and not exceeded) relative to a reference date — Phase 1 uses **Academic year start** (`AcademicYear.StartDate`). Rules are bilingual (Arabic/English explanations), scoped with the same catalog precedence as admission requirements, and published explicitly from the school portal.

Age is **always derived** from `BirthDate + ReferenceDate`. The platform never persists a mutable `Age` field on the child profile.

## Precedence (most specific wins)

Exactly **one** published active rule applies per application selection. Scoring uses the shared `AdmissionScope` helper — there is **no second resolver**. When multiple published rules match, the highest specificity score wins (ties broken by rule `Id`):

| Score | Scope |
|------:|-------|
| 7 | Branch + Grade + Academic year |
| 6 | Branch + Stage + Academic year (no grade) |
| 5 | Grade + Academic year (no branch) |
| 4 | Stage + Academic year (no branch/grade) |
| 3 | Branch + Academic year only |
| 2 | Academic year only |
| 1 | School default (no scope segments) |

**Definition constraint:** Stage and Academic Year are **always required** on a rule (draft and publish). Branch and Grade are optional. Equal specificity at the same `ScopeKey` is blocked at publish (`schoolPortal.ageEligibilityRuleConflict`).

## Completed calendar months

`CompletedCalendarMonths.Compute(birthDate, referenceDate)`:

1. If birth date is after the reference date → `InvalidBirthDate`
2. `months = (ref.Year - birth.Year) * 12 + (ref.Month - birth.Month)`
3. Subtract 1 if the reference day has not yet reached the birth anniversary day in that month
4. **Month-end:** if birth day is 31 and the reference month has fewer days, use the last day of that month (`DateTime.DaysInMonth`) as the effective anniversary
5. **Feb 29:** on non-leap years, the anniversary is **Feb 28** (last valid day of February)

## RuleNotConfigured behavior

When **no** published active rule matches the selection:

- Result code = `RuleNotConfigured`
- `CanContinue = true`
- **Draft creation is not blocked** (no snapshot is created — same pattern as interview/assessment policy)
- Submit is allowed to continue

## Parent application flow

1. **Create draft** — when a published applicable rule exists, a one-per-application snapshot is created (`AgeEligibilitySnapshotCreated`). Missing rule → no snapshot; create succeeds.
2. **Update selection (scope change)** — replace snapshot (`AgeEligibilitySnapshotReplaced`). If a manual exception was approved, clear it and record `AgeEligibilityExceptionInvalidated`.
3. **Submit** — always re-evaluates live `BirthDate`. Blocked when `NotEligible*` / `BirthDateRequired` / `InvalidBirthDate` without a valid manual exception. Allowed when `Eligible`, `RuleNotConfigured`, or `ManualExceptionApproved`.
4. **Get detail** — returns `AgeEligibilityResultDto` from snapshot or live evaluation. Parent `CanRequestAgeException` is always `false` in Phase 1.

## Manual exception

School reviewers with `ManageApplicationReview` may grant an exception when:

- Result is below/above min/max
- Rule has `ManualExceptionAllowed`
- Application status is Draft, Submitted, MissingDocuments, or UnderReview

Idempotent when the same reason is already approved. Scope change invalidates the exception.

## Transfer student module

The Transfer student module **does not exist** yet. This prompt completes **Admission only** and exposes the reusable `IChildAgeEligibilityEvaluator` for future Transfer use. **Do not** create Transfer entities.

## School portal API

Base route: `/api/school-portal/schools/{schoolId}/age-eligibility-rules`

Mutations require CSRF and `SchoolPortal` policy. Permission: `ManageAdmissionRequirements`. Published rules must be **unpublished** before structural edits. `RuleVersion` starts at 1; first publish keeps 1; later publish cycles increment.

Preview: `POST .../age-eligibility-rules/preview` with birth date + scope.

Grant exception: `POST /api/school-portal/schools/{schoolId}/applications/{applicationId}/age-eligibility-exception` (`ManageApplicationReview`).

## Parent / public pre-check

- Parent (preferred): `POST /api/parent/admissions/age-eligibility-check` (CSRF + child ownership)
- Public: `GET /api/schools/{slug}/age-eligibility?branchId&educationalStageId&gradeId&academicYearId&childProfileId?`

See also: [admission-applications.md](./admission-applications.md), [interview-assessment-policy.md](./interview-assessment-policy.md).
