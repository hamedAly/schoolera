# Admission requirements

School-owned admission requirements define what parents must complete before submitting an application. Requirements are bilingual (Arabic/English), scoped, and published explicitly from the school portal.

## Precedence (most specific wins)

For each stable `requirementCode`, at most one **published active** definition applies per application selection. When multiple published definitions match, the highest **specificity score** wins:

| Score | Scope |
|------:|-------|
| 7 | Branch + Grade + Academic year |
| 6 | Branch + Stage + Academic year (no grade) |
| 5 | Grade + Academic year (no branch) |
| 4 | Stage + Academic year (no branch/grade) |
| 3 | Branch + Academic year only |
| 2 | Academic year only |
| 1 | School default (no scope segments) |

Equal specificity for the same code at publish time is blocked (`schoolPortal.admissionRequirementConflict`).

## Requirement kinds

- **InformationalText** — display only; never blocks submit.
- **ParentProfileField** / **ChildProfileField** — validated against live profile at submit.
- **ApplicationDocument** — typed upload slot with allowed extensions, max size, optional vault copy.

## Parent application flow

1. **Create draft** — immutable requirement snapshots are created once from published definitions (`RequirementsSnapshotCreated` history).
2. **Update selection** — snapshots are **not** recreated if they already exist; empty snapshot sets are backfilled once after selection changes.
3. **Submit** — completeness is evaluated using `CultureInfo.CurrentUICulture`. Incomplete required items return `admission.application.requirementsIncomplete` with structured `missingRequirements` in `data` (no submit). Complete path records `RequirementsCompleted` then submits.

Attachments linked to snapshots never expose storage keys in API DTOs.

## School portal API

Base route: `GET/POST /api/school-portal/schools/{schoolId}/admission-requirements`

Mutations require CSRF and `SchoolPortal` policy. Published definitions must be **unpublished** before structural edits.

## Public preview

`GET /api/schools/{slug}/admission-requirements?branchId&educationalStageId&gradeId&academicYearId` returns localized summaries using the same precedence resolver (no internal IDs except document metadata).

See also: [admission-applications.md](./admission-applications.md), [school-portal.md](./school-portal.md).
