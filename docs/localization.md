# Schoolera localization (Arabic + English)

Permanent Phase 1 requirement. This document supersedes older wording that treated English as optional or deferred. Repository rules and this file override future prompt wording that describes English as optional.

## Supported languages

| UI language | Tag | Locale | Direction | Default |
|-------------|-----|--------|-----------|---------|
| Arabic | `ar` | `ar-EG` | RTL | Yes |
| English | `en` | `en-US` | LTR | No |

Fallback language: Arabic (`ar`).

Invalid or missing persisted preferences resolve to Arabic.

## Package choices

Frontend (Angular 22 compatible peers `@angular/core >=16`):

- `@jsverse/transloco` **8.4.x**
- `@jsverse/transloco-locale` **8.4.x**
- `@jsverse/transloco-persist-lang` **8.4.x**
- `@jsverse/transloco-keys-manager` **8.1.x** (dev)

Do **not** install ngx-translate or Angular compile-time `@angular/localize` as a competing translation system.

## Translation file structure

Angular assets root is `Schoolera-SPA/public/` (see `angular.json`).

```text
public/i18n/
├── ar.json                 # shared shell + common keys
├── en.json
├── auth/{ar,en}.json       # Transloco scope: auth
├── schools/{ar,en}.json    # Transloco scope: schools
└── admin/{ar,en}.json      # Transloco scope: admin
```

Loader path: `./i18n/${langPath}.json`.

### Key naming

Prefer semantic keys:

- `nav.home`
- `common.actions.save` / `common.save`
- `schools.form.name`
- `validation.required`
- `titles.schools`

Shared shell strings live in root files. Large lazy features may use scopes. Do not create one JSON file per tiny component.

Homepage marketing copy lives under the root `home.*` tree in `public/i18n/{ar,en}.json` (not a separate Transloco scope).

## Language service responsibilities

`DocumentLanguageService` owns:

- Supported languages
- Current language / locale / direction
- Default + fallback
- Validation of persisted values
- Transloco `setActiveLang`
- `document.documentElement` `lang` + `dir`
- Accept-Language short tag (`ar` | `en`)

Switching language:

- Does **not** reload the application
- Preserves the current Angular route
- Persists to `localStorage` key `schoolera.lang`

Startup flash mitigation: `index.html` defaults to `lang="ar"` `dir="rtl"`; persist plugin + `getLangFn` normalize invalid cached values to Arabic before render.

## Language switcher

`se-language-switcher`:

- Arabic active → shows **English**
- English active → shows **العربية**
- Accessible button + labels
- Emits `languageChange` so the public mobile menu can close
- Used in desktop header actions and mobile drawer
- No `href="#"`

## Locale formatting

`LocaleFormatService` centralizes:

- Dates / date-time
- Numbers
- Currency (requires API currency **code**, never a hardcoded symbol)
- Percentages

Mappings: `ar → ar-EG`, `en → en-US`.

Use Transloco params for sentence-like summaries (`format.resultCount`, `format.paginationSummary`) instead of concatenating fragments.

## Accept-Language interceptor

`acceptLanguageInterceptor` adds `Accept-Language: ar|en` to **all** `HttpClient` requests, including NSwag-generated clients.

- Do not edit `SwaggerClient.service.ts`
- Do not set the header inside feature services
- Keep the existing HTTP error interceptor

## ASP.NET Core RequestLocalization

- `AddLocalization()`
- Supported cultures: `ar-EG`, `en-US`, `ar`, `en`
- Default culture: `ar-EG`
- `AcceptLanguageHeaderRequestCultureProvider` first
- Placed **before** exception middleware so validation/error messages see the request culture

Preserves Swagger, NSwag, static SPA hosting, uploads, and API/Swagger/upload 404 isolation.

## Backend resources and error codes

- Display strings: `IStringLocalizer<ApiMessages>` / `IStringLocalizer<ValidationMessages>`
- Stable codes: `ErrorCodes` (`error.unexpected`, `error.validation`, `error.not_found`, `error.unauthorized`, `error.forbidden`)
- Auth codes: `AuthErrorCodes` (`auth.invalidCredentials`, `auth.accountSuspended`, `auth.accountNotVerified`, `auth.emailAlreadyExists`, `auth.duplicatePhone`, `auth.invalidVerificationCode`, `auth.expiredVerificationCode`, `auth.rateLimited`, `auth.invalidResetToken`, `auth.unauthorized`, `auth.forbidden`, password policy codes such as `auth.passwordRequiresUppercase`, etc.) — see `docs/authentication.md`
- `Result<T>` keeps localized `errors` **and** stable `errorCodes`
- Frontends must branch on `errorCodes`, never on Arabic/English message text
- Do not expose exception details or put database content into `.resx` files

## UI translations vs bilingual database content

| Kind | Storage | Notes |
|------|---------|-------|
| Static UI chrome | Transloco JSON | Buttons, nav, validation chrome, titles |
| User-entered free text | As entered | No machine translation |
| Managed public content | DB fields `NameAr`/`NameEn`, `TitleAr`/`TitleEn`, … | Explicit bilingual columns when intended for public display |

Do **not** add speculative bilingual columns until a real feature needs them.

### Future API DTO rules

- **Public** APIs: return localized display DTOs according to `Accept-Language` where appropriate
- **Management** APIs: return **both** Arabic and English field values
- Never expose EF entities from controllers

CMS pages, FAQs, and homepage content apply these rules now: both Arabic and English values are required before publication, admin DTOs return both languages, and public content DTOs return the localized value selected from `Accept-Language` with Arabic fallback. See [cms-and-contact.md](./cms-and-contact.md).

### Bilingual field component

`se-bilingual-field-group`:

- Binds to an existing `FormGroup`
- Shows Arabic (RTL) + English (LTR) fields together
- Side-by-side on desktop, stacked on mobile
- Labels/validation via translation keys
- Does **not** hide a content language based on UI language
- Development showcase route: `/dev/bilingual-fields` (no migration)

## Commands

```powershell
cd src/Schoolera.Api/Schoolera-SPA
npm run i18n:extract
npm run i18n:check      # tree parity ar/en (fails on mismatch)
npm run i18n:find       # keys-manager find
npm run i18n:validate   # check + find
npm run i18n:all        # extract + validate
```

Localization validation must pass before a feature is considered complete.

## Required tests (minimum)

Frontend: default Arabic, restore valid lang, invalid → Arabic, dir/lang switches, route preserved, desktop/mobile switcher, menu closes, Accept-Language header, bilingual field both columns, key parity script.

Backend: culture selection from Accept-Language, validation messages localized, unexpected errors safe + coded, SPA/API/Swagger/upload isolation unchanged.

## Known SEO limitations

No SSR/SSG yet. Browser titles are client-translated via Transloco title strategy. Crawlers that do not execute JS may see the default document title only.

## How to add a translation key

1. Add the key to **both** `ar.json` and `en.json` (or the matching scope pair).
2. Use `{{ 'my.key' | transloco }}` or `TranslocoService`.
3. Run `npm run i18n:check`.

## How to add a bilingual entity field (future)

1. Add `*Ar` and `*En` columns via a named EF migration.
2. Expose both on management DTOs; localize on public DTOs.
3. Use `se-bilingual-field-group` in management forms.
4. Do not put entity content into Transloco files.
