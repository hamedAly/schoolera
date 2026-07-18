# Interview FAQs (Phase 1.4)

## Scope

Interview/Assessment FAQs extend the existing `FaqItem` / `FaqCategory` CMS model. There is **no** second FAQ entity and **no** FAQ snapshots on admission applications.

`InterviewCategory == null` means a general CMS FAQ (parents / schools / platform homepage FAQ page).
`InterviewCategory != null` means an Interview/Assessment FAQ.

## Ownership

| `OwnershipScope` | `SchoolId` | Applicability IDs | Who manages |
|------------------|------------|-------------------|-------------|
| `Platform` (1) | must be null | must be null | Platform Admin CMS |
| `School` (2) | required | optional wildcards | School portal (`ManageContent`) |

School applicability fields (`SchoolBranchId`, `EducationalStageId`, `GradeId`, `AcademicYearId`) are null-as-wildcard. Platform interview FAQs never carry applicability IDs.

## Public ordering

For `GET /api/schools/{slug}/interview-faqs` and parent application detail:

1. Platform interview FAQs (`SortOrder`, then `Id`)
2. School interview FAQs for that school (`SortOrder`, then `Id`)

Only published + active interview FAQs are returned. Draft/inactive items are excluded.

Applicability uses `AdmissionScope.MatchesScope` when branch/stage/grade/year are all present. If a context dimension is missing, the FAQ is included only when that definition dimension is also null.

## Permissions

- Platform Admin: `GET/POST/PUT` under `/api/admin/cms/faq` including `interview-items`, activate/deactivate.
- School portal: `/api/school-portal/schools/{schoolId}/interview-faqs` requires `SchoolPortalPermission.ManageContent` (Owner / SchoolAdmin / ContentModerator). AdmissionOfficer is denied writes.
- Mutations require CSRF (`X-XSRF-TOKEN`).
- School create forces `OwnershipScope = School` and ignores any client `SchoolId` tampering (route `schoolId` is authoritative).

## General FAQ page

`GET /api/content/faqs` excludes items where `InterviewCategory != null` **or** `OwnershipScope == School`, so the public/general FAQ page stays general-only.

## No snapshots

Interview FAQ content is always resolved live from `FaqItem`. Admission applications do not store FAQ copies. Editing FAQs does not emit parent notifications.

## Container category

Interview FAQs are stored under the seeded FAQ category slug `interview-assessment` (created on demand if missing). That category is not shown as a general public FAQ group when it only contains interview items.
