# School catalog and taxonomies

## Domain model

Schoolera uses explicit taxonomy entities (Country, Governorate, City, District, Curriculum, EducationalStage, Grade, Facility, AcademicYear) and a rich School aggregate with branches, curricula, stage/grade offerings, tuition fees, facilities, and images.

### Location hierarchy (Phase 1)

```
Country → Governorate → City → District
```

- `District` remains the lowest location level (no separate Area entity).
- `City.GovernorateId` is **nullable** so legacy/unmapped cities are preserved safely.
- Angular resolves Egypt by stable code **`EG`**, never by hardcoded database IDs.

### Stage/grade offerings

Offerings are normalized at the branch level:

- `SchoolStageOffering` — branch + educational stage + gender + capacity + admission-open flag
- `SchoolGradeOffering` — grades available within a stage offering

This avoids duplicating stage/grade data and supports public filtering by admission status.

### Facilities

Facilities are assigned at the **school level** via `SchoolFacility` (not per branch). Public profiles list school-wide amenities.

### Slugs

`SlugHelper` (Domain) normalizes Latin text to kebab-case slugs. Arabic-only names receive a stable `item-{hash}` slug. Slugs are unique where required and used in public URLs (`GET /api/schools/{slug}`).

## Public APIs

| Route | Purpose |
|-------|---------|
| `GET /api/taxonomies/countries` | Active countries (`CountryTaxonomyItemDto` includes `code`) |
| `GET /api/taxonomies/countries/{countryId}/governorates` | Active governorates in a country |
| `GET /api/taxonomies/governorates/{governorateId}/cities` | Active cities in a governorate |
| `GET /api/taxonomies/cities` | Active cities (legacy-compatible flat list) |
| `GET /api/taxonomies/cities/{cityId}/districts` | Districts in a city |
| `GET /api/taxonomies/curricula` | Active curricula |
| `GET /api/taxonomies/educational-stages` | Active stages |
| `GET /api/taxonomies/educational-stages/{stageId}/grades` | Grades in a stage |
| `GET /api/taxonomies/facilities` | Active facilities |
| `GET /api/taxonomies/academic-years` | Active academic years |
| `GET /api/schools` | Paged published school search (`PagedResult<PublicSchoolListItemDto>`) |
| `GET /api/schools/{slug}` | Published school profile (see [public-school-profile.md](./public-school-profile.md)) |
| `GET /api/schools/{slug}/related` | Related published school cards |
| `POST /api/schools/{slug}/contact-leads` | Public contact/interest lead (CSRF + rate limit) |

Public read DTOs localize display names via `Accept-Language` (`LocalizationDisplayHelper`).

### `GET /api/schools` search contract

Query parameters (ASP.NET Core model binding; repeated values for collections):

| Parameter | Notes |
|-----------|--------|
| `search` | Trimmed; empty/whitespace ignored; matches NameAr/NameEn and short descriptions |
| `countryId` | Active branch city mapped to a governorate in this country |
| `governorateId` | Active branch city in this governorate |
| `cityId` | Active branch city (preserved; bookmarked URLs remain valid) |
| `districtId` | Active branch district; must belong to `cityId` when both set |
| `curriculumId` / `curriculumIds` | **ANY** matching active curriculum |
| `stageId` / `gradeId` | Active offerings; grade must belong to stage when both set |
| `schoolType` / `genderType` | Enum int values |
| `admissionOpen` | At least one matching **active** stage offering with `IsAdmissionOpen` on a published school (see **Admission open** below) |
| `facilityIds` | **ALL** selected active facilities required |
| `minimumTuition` / `maximumTuition` | Non-negative; min ≤ max; **EGP** only |
| `academicYearId` | Fee year; defaults to current active academic year when omitted |
| `latitude` / `longitude` | Required for `sort=nearest`; ranges −90…90 / −180…180 |
| `sort` | See below |
| `pageNumber` / `pageSize` | `PagedRequest` (default 20, max 100) |

Combined location filters are validated (district↔city, city↔governorate, governorate↔country). Schools with multiple matching branches appear once.

**Public visibility:** only `SchoolStatus.Published`. Unpublished (including onboarding-approved unpublished) and suspended schools are excluded.

### Admission open

Catalog `isAdmissionOpen` / `admissionOpen` is **not** derived from `SchoolStatus` alone. A school is treated as admission-open for discovery when it is **Published** and has at least one **active** branch stage offering with `IsAdmissionOpen` (and matching search filters when applied).

**Applying** (Parent Admission Applications) requires the full eligibility set: published school + active branch + active stage offering with `IsAdmissionOpen` + active grade offering + active academic year + school/offering gender eligibility for the child. Details: [admission-applications.md](./admission-applications.md).

**Tuition:** when a tuition filter is applied, schools without a matching active EGP fee for the resolved academic year are excluded. Without a tuition filter, schools with null fees remain visible. Null-fee schools sort after fee schools for lowest/highest-fee.

**Sort values:** `relevance` | `name-asc` | `name-desc` | `lowest-fee` | `highest-fee` | `newest` | `nearest`

- `relevance` without search falls back to `newest`
- `nearest` without valid coordinates → `400` validation error
- `nearest` ranks matching schools by equirectangular distance (candidate IDs loaded then sorted; suitable for Phase 1 catalog size). `distanceKm` is returned when coordinates are supplied

**Card DTO fields:** `id`, `slug`, `name`, `city`, `district`, `logoUrl`, `coverUrl`, `schoolType`, `genderType`, `isAdmissionOpen` (derived from active open offerings on published schools — not `SchoolStatus` alone), `curriculumSummary`, `educationalStageSummary`, `minimumAnnualFee`, `feeCurrency`, `distanceKm`, `badges` (admission-open, newly-added ≤30 days, school-type, gender). No fake ratings/featured/accreditation badges.

Homepage Featured Schools continues to call `GET /api/schools?pageNumber=1&pageSize=4` (newest default sort).

Angular `/schools` owns filter state in the URL query string (`SchoolsApi` → NSwag → this endpoint).

## Admin taxonomy APIs

Prefix: `/api/admin/taxonomies`

Location writes (PlatformAdmin + CSRF):

- Countries: `POST/PUT /countries`, `POST /countries/{id}/deactivate`
- Governorates: `POST/PUT /governorates`, `POST /governorates/{id}/deactivate`
- Cities: `POST/PUT /cities` (create requires `governorateId`), `POST /cities/{id}/deactivate`
- Districts: `POST/PUT /districts`, `POST /districts/{id}/deactivate`

All write operations require **`PlatformAdminOnly`** policy, cookie authentication, and CSRF (`ValidateAntiForgeryToken`).

Prefer deactivation over deletion when taxonomies are referenced.

## Development seed

`SchoolCatalogSeeder` (idempotent by slug/code) seeds:

| Layer | Coverage |
|-------|----------|
| Country | Egypt (`EG`) — complete |
| Governorates | All **27** Egyptian governorates — complete |
| Cities | **Demo-only**: Cairo, Giza, Alexandria (IDs preserved; mapped to matching governorates) |
| Districts | **Demo-only** sample districts under those cities |

Unmapped cities are never assigned a fake governorate. Existing City/District IDs are preserved; Admin-edited names are not overwritten on re-seed.

Runs when `Database:SeedData=true` after migrations.

### Browser geolocation (Angular search)

School Search may request browser coordinates **only after** an explicit user click. Coordinates are URL-owned for nearest sort, never stored in Parent Profile, localStorage, audit logs, or the database. No reverse geocoding is used.

### Map view (Prompt 13)

List/Map toggle on `/schools` (`view=map`). Map pins come from `GET /api/schools/map-pins` (Branch-level). Public-safe Map config: `GET /api/schools/map-configuration` (DB-managed `IntegrationType.Map`). See [map-based-school-search.md](./map-based-school-search.md). Product tile provider approval is **blocked for Production**.

## Indexes

Additive indexes for public search (`AddPublicSchoolSearchIndexes`):

- `Schools (Status, CreatedAtUtc)`, `Schools (Status, NameAr)`
- `SchoolBranches (IsActive, CityId, DistrictId)`

Map search adds `SchoolBranches (IsActive, Latitude, Longitude)` (`AddMapSearchBranchCoordinateIndex`). **No** SQL Server spatial/`geography` / NetTopologySuite in Phase 1.

## Angular data access

- `SchoolsApi` wraps NSwag `Client` school endpoints; maps paged list to featured-school consumption on the homepage and full search on `/schools`.
- `TaxonomiesApi` wraps public taxonomy endpoints.

Do not call generated clients directly from components.
