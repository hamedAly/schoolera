# Public school profile experience

## Purpose

Parents and anonymous visitors can open a **published** school’s public profile at `/schools/{slug}`, review structured catalog data, browse related schools, and submit a contact/interest lead.

This module extends the existing `GET /api/schools/{slug}` contract. It does **not** add Admissions, Favorites persistence, SSR, external map providers, or a lead-management console.

## Public visibility

Only `SchoolStatus.Published` schools are returned.

| Condition | HTTP |
|-----------|------|
| Published slug | `200` |
| Unknown slug | `404` `school.not_found` |
| Unpublished / Suspended / Draft | `404` `school.not_found` (non-enumerating) |

Slug validation uses `SlugHelper.IsValidSlug`. Nested collections include **active** public records only (branches, offerings, grades, fees, images, facilities, curricula, additional services).

## Profile API

`GET /api/schools/{slug}` → `Result<PublicSchoolProfileDto>`

Includes identity/header fields, contact, SEO display fields, branches, curricula, facilities, gallery images (optional `educationalStageId`), stage offerings (with `branchId` / `branchName`), tuition fees, additional services, and derived `isAdmissionOpen` from active open offerings (not from `SchoolStatus` alone).

Does **not** expose owner id, internal status notes, team members, or onboarding private data.

Profile responses enqueue a best-effort daily view aggregate (see Analytics). Analytics failure never fails the HTTP response.

## Related schools

`GET /api/schools/{slug}/related?limit=`

- Default limit `4`, maximum `8`
- Published only; excludes current school
- Reuses `PublicSchoolListItemDto` card contract
- Ordering: same district → same city → shared stage → shared curriculum → optional coordinate distance when both have coords → localized name / created-at tie-breakers

## Contact leads

`POST /api/schools/{slug}/contact-leads` (anonymous, CSRF required, rate-limited)

Body (`SchoolContactLeadRequest`):

- `name`, `phone` (required, bounded)
- `email`, `message` (optional, bounded)
- `consentAccepted` must be `true` → `school.contact.consent_required`
- `source` allowlist: `school-profile` | `related-school-card` | `search-result` → `school.contact.invalid_source`
- `website` honeypot must be empty → `school.contact.rejected`

Persisted entity: `SchoolContactLead` (school id, source, name, phone, email, message, consent, culture, created timestamp).

**Not stored:** raw IP, cookies, tokens, user-agent, full headers.

Rate limit policy `schools-contact-lead`: 20 requests / minute partitioned by client IP + slug. Additional 2-minute identical phone+school cooldown → `school.contact.rejected`.

Success returns `SchoolContactLeadResultDto` (`leadId`, `submittedAtUtc`, localized message) without stored PII echo.

Lead review/management UI is **out of scope** for this prompt.

## Profile-view analytics

Entity `SchoolProfileViewDaily` unique on `(SchoolId, ViewDateUtc)` stores aggregate counts only.

In-process bounded `Channel` (capacity 2000, `DropWrite`) + `SchoolProfileViewBackgroundService` performs SQL Server `MERGE` upserts. Full queue drops are logged without visitor identity. Best-effort in-process delivery — not a distributed analytics platform.

## Angular

- Route: existing `/schools/:slug` (`SchoolDetailsPage`)
- Data access: `SchoolsApi` → NSwag `Client.schoolsGET5` / `related` / `contactLeads`
- Apply CTA (`schools.profile.applyNow`) when `environment.features.admissionsEnabled`, `isAdmissionOpen`, and admission-open offerings include stage/grade ids; see [admission-applications.md](./admission-applications.md)
- Favorites button hidden when `environment.features.favoritesEnabled === false` (default)
- Offerings DTO includes `educationalStageId` for Parent Apply selection (public, non-sensitive)
- Runtime SEO via `SeoService` (title, description, OG, Twitter, canonical, JSON-LD). Canonical uses `environment.publicSiteBaseUrl` (Production must not invent Host-header origins). Runtime JSON-LD may not be processed by all crawlers without SSR.
- No Google Maps / Mapbox integration; coordinates shown as text when valid

## Migration

`20260716182358_AddPublicSchoolProfileLeadsAndViews` — tables `SchoolContactLeads`, `SchoolProfileViewDaily`.
