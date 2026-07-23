# Schoolera SPA

Standalone Angular application for Schoolera. It remains a normal Angular app (`projectType: application`) and is hosted in production by `Schoolera.Api` as static files from `wwwroot`.

## API contract (NSwag)

- Official client: `src/app/core/api-client/SwaggerClient.service.ts` (auto-generated; **never edit manually**).
- Feature services (for example `SchoolsApi`) wrap generated clients; components do not call generated clients directly.
- Regeneration: start `Schoolera.Api` in Development with `Nswag:Enabled=true`.

## Production / same-origin

- Build output is written to `../wwwroot` (ASP.NET Core static files root).
- NSwag `API_BASE_URL` is empty (`''`); generated paths are already `/api/...`.
- ASP.NET Core maps controllers first, then falls back unmatched routes to `index.html`.

```bash
# Use Node from .nvmrc (24.18.0) before install/build
npm ci
npm run build
```

Then run the API project and browse the API origin.

## Development with `ng serve`

1. Start the API on **HTTP** (not the dual HTTPS profile):

```bash
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --launch-profile http
# or: --urls http://127.0.0.1:5085
```

2. Start Angular (proxy forwards `/api` to `:5085`):

```bash
cd 'D:\Test2\School\Project\src\Schoolera.Api\Schoolera-SPA'
npm start -- --port 5100
# or
npm start -- --port 4201 --host 127.0.0.1
```

`proxy.conf.json` forwards `/api` and `/swagger` to `http://127.0.0.1:5085`. Relative `/api/...` requests are used (no localhost in generated client source). Development CORS (`Cors:AllowedOrigins` + credentials) covers direct cross-origin calls if needed; prefer the proxy so cookies and CSRF stay same-origin with the SPA.

## Routing (public shell)

Public pages use a nested `PublicLayout` (skip link, header, main, footer). Temporary operations modules (`/dashboard`, `/students`, `/staff`, `/classes`) keep the separate `AppShell`.

| Route | Purpose |
|-------|---------|
| `/` | Public bilingual homepage (hero, trust, benefits, journey, featured schools, FAQ, CTAs) |
| `/schools` | Public school search (URL query state, filters, sort, pagination) |
| `/schools/:slug` | Public school profile (hero, sections, gallery, contact lead, related, runtime SEO) |
| `/about`, `/how-it-works`, `/privacy`, `/terms`, `/sla` | Published bilingual CMS pages |
| `/faq` | Published bilingual FAQ categories/items with runtime FAQ JSON-LD |
| `/contact` | Public contact form |
| `/auth/account-type` | Choose Parent or Educational Institution |
| `/auth/login`, `/auth/register/parent`, `/auth/register/school-owner` | Login and registration |
| `/auth/verify`, `/auth/forgot-password`, `/auth/reset-password` | Verification and password reset |
| `/parent` | Redirects to `/parent/dashboard` (Parent role) |
| `/parent/dashboard`, `/parent/profile`, `/parent/children` | Parent portal (dashboard, profile, children) |
| `/parent/applications`, `/parent/applications/new`, `/parent/applications/:id`, `/edit`, `/success` | Parent admission applications |
| `/parent/children/new`, `/parent/children/:childId/edit` | Add / edit child |
| `/school`, `/admin`, `/support` | Protected portal shells / placeholders |
| `/school/onboarding`, `/school/onboarding/status` | SchoolOwner institution onboarding wizard and status |
| `/school/:schoolId/overview`, `profile`, `branches`, `stages`, `fees`, `facilities`, `gallery`, `services`, `team` | School portal management (SchoolOwner/SchoolAdmin) |
| `/school/:schoolId/applications`, `/school/:schoolId/applications/:applicationId` | School admission application review |
| `/admin/dashboard`, `/admin/onboarding`, `/admin/schools`, `/admin/users`, `/admin/taxonomies`, `/admin/audit` | Platform Admin console |
| `/admin/applications`, `/admin/applications/:applicationId` | Platform Admin admission monitoring (read-only) |
| `/admin/cms/pages`, `/admin/cms/pages/new`, `/admin/cms/pages/:pageId` | CMS page management |
| `/admin/cms/faq`, `/admin/cms/home` | FAQ and structured homepage management |
| `/admin/contact-requests`, `/admin/contact-requests/:requestId` | Contact request monitoring and status workflow |
| `/unauthorized` | Permission denied page |
| `/**` (Angular) | Arabic/English 404 page |

## Localization

Arabic (`ar`, RTL default) and English (`en`, LTR) via Transloco. Translation files: `public/i18n/`. See `docs/localization.md`.

Node: use the version in `.nvmrc` / `.node-version` (**24.18.0**).

```powershell
npm run i18n:check
npm run i18n:validate
npm run i18n:all
```

`i18n:find` runs the Angular 22-compatible scanner `tools/i18n-find.mjs` (official keys-manager find is not used for Angular 22). Parity remains mandatory.

Document language/direction are applied at runtime from the active language (default Arabic). Development bilingual field showcase: `/dev/bilingual-fields`.

## Authentication

- `AuthApi` wraps auth JSON POST endpoints with `withCredentials`.
- `AuthService` initializes session via `GET /api/auth/me`; no tokens in browser storage.
- Guards: `authGuard`, `roleGuard`, `guestGuard`; safe `returnUrl` via `sanitizeReturnUrl`.
- CSRF: Angular `withXsrfConfiguration` + credentials interceptor.
- Validation UX: branch on `errorCodes` only; `se-form-error-summary`, field errors, and `ToastService` (`se-toast-host`).
- Email verification: after register, `/auth/verify` shows a masked email, delivery hints (including DevelopmentLog), 6-digit OTP input, and resend cooldown. In Development, codes appear in the **API console**, not in the browser. See `docs/authentication.md`.
- Never show raw Identity English messages.

## Homepage assets

Original homepage assets under `public/assets/home/`:

- `school-discovery-hero.svg` — original isometric school-campus hero illustration (640×520, no baked-in text, `aria-hidden` decorative SVG behind an `alt` image).
- `parent-family.jpg` — original generated family photo for the Parent Benefits section (1536×1024, lazy-loaded).

Do not hotlink or copy third-party marketing assets.

## Public school profile

- Route `/schools/:slug` loads `GET /api/schools/{slug}` via `SchoolsApi` → NSwag.
- Related schools and contact leads use generated `Client.related` / `Client.contactLeads`.
- Feature flag `environment.features.favoritesEnabled` (default `false`) hides Favorites.
- Feature flag `environment.features.admissionsEnabled` (default `true`) gates School Profile Apply CTA and Parent admission UI availability messaging.
- Canonical/OG URLs use `environment.publicSiteBaseUrl` (set in deployment; Development may use `http://localhost:5100`).
- See `docs/public-school-profile.md`.

## Parent portal

- Routes under `/parent/**` require authentication and the Parent role (`roleGuard`).
- `ParentApi` wraps NSwag `Client` parent methods (`dashboard2`, `profileGET`/`PUT`, `children*`, `admissionApplications*`, `submit`, `cancel`, attachments) plus HttpClient upload progress / blob download for attachments.
- Transloco scope `parent` (`public/i18n/parent/{ar,en}.json`).
- Child identity is shown masked only; full values are never stored in `localStorage`.
- Admission routes: `/parent/applications`, `/new`, `/:applicationId`, `/:applicationId/edit`, `/:applicationId/success`. Detail shows review outcome timestamps and `parentVisibleRejectionReason` when applicable.
- See `docs/parent-dashboard.md` and `docs/admission-applications.md`.

## School portal

- Routes under `/school/:schoolId/**` require authentication and SchoolOwner or SchoolAdmin (`roleGuard` + `schoolPortalContextGuard`).
- `SchoolPortalApi` wraps NSwag portal methods plus HttpClient for media upload progress and admission attachment blob download.
- Admission routes: `/school/:schoolId/applications`, `/school/:schoolId/applications/:applicationId` (list, review, accept/reject).
- Transloco scope `portal` (`public/i18n/portal/{ar,en}.json`).
- See `docs/school-portal.md` and `docs/admission-applications.md`.

## Platform Admin

- Routes under `/admin/**` require authentication and PlatformAdmin (`roleGuard`).
- `AdminPlatformApi` wraps NSwag admin methods plus HttpClient for admission CSV export.
- `AdminCmsApi` wraps the generated `/api/admin/cms/**` contract for pages, FAQs, and homepage content.
- Admission monitoring routes: `/admin/applications`, `/admin/applications/:applicationId` (read-only list/detail; CSV export from list).
- CMS routes: `/admin/cms/pages/**`, `/admin/cms/faq`, and `/admin/cms/home`.
- Contact monitoring routes: `/admin/contact-requests`, `/admin/contact-requests/:requestId`.
- Transloco scope `admin` (`public/i18n/admin/{ar,en}.json`).
- See `docs/platform-admin.md`, `docs/admission-applications.md`, and `docs/cms-and-contact.md`.

## Public CMS and contact

- `PublicContentApi` wraps generated clients for `/api/content/pages/{slug}`, `/api/content/faqs`, `/api/content/home`, and `POST /api/contact`.
- CMS/public pages consume localized DTOs selected by `Accept-Language`; admin CMS forms keep Arabic and English fields together.
- The contact form uses the normal Angular XSRF setup; the API additionally enforces rate limiting, honeypot, and duplicate-cooldown checks.
- See `docs/cms-and-contact.md`.

## Notes

- This folder is the Angular project root (`angular.json`, `package.json`, `src/`).
- Do not treat this as an Angular library project.
