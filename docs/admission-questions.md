# Admission questions

School-owned dynamic admission questions collect parent answers during the draft application wizard. Questions are bilingual (Arabic/English), scoped, and published explicitly from the school portal.

## Precedence (most specific wins)

For each stable `questionCode`, at most one **published active** definition applies per application selection. When multiple published definitions match, the highest **specificity score** wins — same table as [admission-requirements.md](./admission-requirements.md):

| Score | Scope |
|------:|-------|
| 7 | Branch + Grade + Academic year |
| 6 | Branch + Stage + Academic year (no grade) |
| 5 | Grade + Academic year (no branch) |
| 4 | Stage + Academic year (no branch/grade) |
| 3 | Branch + Academic year only |
| 2 | Academic year only |
| 1 | School default (no scope segments) |

Equal specificity for the same code at publish time is blocked (`schoolPortal.admissionQuestionConflict`).

## Question types

- **ShortText** / **LongText** — free text with optional min/max length.
- **SingleChoice** / **MultipleChoice** — option codes snapshotted with the question.
- **Date** — optional min/max date bounds.
- **YesNo** — boolean answer.
- **File** — private attachment linked to the question snapshot (allowed extensions + max size).

## Parent application flow

1. **Create draft** — immutable question snapshots are created once from published definitions (`QuestionsSnapshotCreated` history), alongside requirements.
2. **Questions step** — `POST .../question-snapshots/ensure` is idempotent; snapshots are not recreated unless scope changes with confirmation.
3. **Answers** — `PUT .../answers` upserts one snapshot answer; file questions use `POST .../attachments` with `questionSnapshotId` (mutually exclusive with `requirementSnapshotId`).
4. **Update selection** — changing branch/stage/grade/year while answers exist returns `admission.application.questionAnswersBlockScopeChange` unless `confirmClearQuestionAnswers` is true; confirmed scope changes reset snapshots/answers (`QuestionsSnapshotReset`).
5. **Submit** — after requirements pass, question completeness is evaluated using `CultureInfo.CurrentUICulture`. Incomplete required questions return `admission.application.questionsIncomplete` with structured `missingQuestions` in `data`.

Attachments and storage keys are never exposed in API DTOs.

## School portal API

Base route: `GET/POST /api/school-portal/schools/{schoolId}/admission-questions`

Mutations require CSRF and `SchoolPortal` policy. Published definitions must be **unpublished** before structural edits.

## Admin CSV export

`GET /api/admin/admission-applications/export?includeAnswers=true` appends up to **30** dynamic `Answer:{questionCode}` columns. Answers load only when `includeAnswers=true`.

See also: [admission-requirements.md](./admission-requirements.md), [admission-applications.md](./admission-applications.md).
