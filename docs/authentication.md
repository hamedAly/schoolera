# Schoolera authentication (Phase 1)

## Scheme

ASP.NET Core **Identity** with **cookie-based** authentication (`Schoolera.Auth` HttpOnly cookie). No access/refresh tokens in browser storage.

| Environment | Cookie secure | SameSite |
|-------------|---------------|----------|
| Development | `SameAsRequest` (HTTP via Angular proxy works) | `Lax` |
| Production | `Always` (HTTPS required) | `Lax` |

Sliding expiration is enabled (default 14 days). Session renewal uses the cookie pipeline — no refresh-token entities.

## CSRF

State-changing cookie-authenticated `/api/**` endpoints (auth, school portal, admin, onboarding) use `[ValidateAntiForgeryToken]`. The API registers `AddControllersWithViews()` so the antiforgery MVC filter is available.

- Server antiforgery cookie: HttpOnly (framework default name)
- SPA cookie: `XSRF-TOKEN` (readable by Angular) — request token
- Header: `X-XSRF-TOKEN`
- Issued on `GET /api/auth/me` by `AntiforgeryCookieMiddleware` (`GetAndStoreTokens` + append request-token cookie)
- Angular: `withXsrfConfiguration` + `withCredentials` on `/api` requests

Rejected requests return `400` with `error.validation`.

API `401`/`403` from cookie authorization return JSON `Result<T>` with `auth.unauthorized` / `auth.forbidden` (written in application-cookie redirect events for `/api/**`).

## Development connectivity (Angular ↔ API)

| Piece | Configuration |
|-------|----------------|
| Angular | Relative `/api` (`apiBaseUrl: ''`) + `proxy.conf.json` → `http://127.0.0.1:5085` |
| API listen | Prefer `http` launch profile / `http://localhost:5085` (not HTTPS redirect to `:7076`) |
| `HttpsRedirection:Enabled` | `false` in Development; `true` in Production |
| `Cors:AllowedOrigins` | Exact origins only (`http://localhost:5100`, etc.); credentials allowed; Development-only policy |

Do not hardcode frontend origins in `Program.cs` or absolute API URLs in Angular feature code. Registration and other cookie/CSRF POSTs must stay on the SPA origin (proxied) so cookies and `X-XSRF-TOKEN` remain same-site.

## User model

`ApplicationUser` (Identity `Guid` key) fields:

- FirstName, LastName, Email, PhoneNumber
- PreferredLanguage (`ar` / `en`)
- AccountStatus: `PendingVerification`, `Active`, `Suspended`
- CreatedAtUtc, UpdatedAtUtc, LastLoginAtUtc

Unique normalized email (Identity) and unique phone index.

## Public registration

Only **Parent** and **SchoolOwner** via:

- `POST /api/auth/register/parent`
- `POST /api/auth/register/school-owner`

Privileged roles cannot be created through public endpoints.

## Email verification

### Flow

1. Parent/SchoolOwner registration creates an Identity user with `AccountStatus=PendingVerification` and `EmailConfirmed=false`.
2. `VerificationCodeService.IssueRegistrationCodeAsync` generates a cryptographically random **6-digit** numeric code (`Auth:Verification:CodeLength`).
3. Previous unconsumed registration codes for that user are marked consumed.
4. The **SHA-256 hash** of the code is stored in `VerificationCodes` (`CodeHash`), with `ExpiresAtUtc` = now + `Auth:Verification:ExpirationMinutes` (default 15).
5. `ITransactionalEmailSender` delivers to the **exact registration email** (`ApplicationUser.Email`).
6. Angular navigates to `/auth/verify?email=...` with router state describing delivery success/mode.
7. `POST /api/auth/verify` validates email + code, then sets `EmailConfirmed=true` and `AccountStatus=Active`, and consumes the code.
8. `POST /api/auth/resend-verification` enforces cooldown (`Auth:Verification:ResendCooldownSeconds`, default 60), issues a new code (invalidating the previous), and delivers again. Unknown/already-active accounts receive a generic success message (no enumeration).

### Why users previously could not obtain a code

No email provider existed. Development only wrote the code to the **API console log**. The Angular verify page did not explain that. Arbitrary values like `12345678` fail FluentValidation (`Length(6)` → `error.validation`); wrong 6-digit values return `auth.invalidVerificationCode`.

### Delivery modes (`Email` configuration)

| Mode | Environment | Behavior |
|------|-------------|----------|
| `DevelopmentLog` | Development only | Does **not** send SMTP mail. Logs recipient + code to the API console with a clear Development banner. |
| `Smtp` | Development or Production | Sends a bilingual plain-text email via configured SMTP (e.g. Mailpit on `127.0.0.1:1025`). |

Production **rejects** `DevelopmentLog` at options validation / startup.

```json
"Email": {
  "Mode": "DevelopmentLog",
  "FromName": "Schoolera",
  "FromAddress": "no-reply@schoolera.local",
  "Smtp": {
    "Host": "127.0.0.1",
    "Port": 1025,
    "UseSsl": false,
    "Username": "",
    "Password": ""
  }
}
```

SMTP credentials must come from User Secrets or environment variables (`Email__Smtp__Password`, etc.). Never commit real passwords.

**Account created but delivery failed:** registration still succeeds (no duplicate user on retry of the same email — duplicate email is rejected). Response includes `verificationDeliverySucceeded: false`. User can open verify page and use Resend.

### Verification error codes

| Code | Meaning |
|------|---------|
| `auth.invalidVerificationCode` | Wrong code / unknown email |
| `auth.expiredVerificationCode` | Code past `ExpiresAtUtc` |
| `auth.codeAlreadyUsed` | Matching code already consumed |
| `auth.emailAlreadyVerified` | Account already active/confirmed |
| `auth.resendTooSoon` | Resend cooldown |
| `auth.deliveryFailed` | SMTP/delivery failure on resend |
| `error.validation` | Request shape invalid (e.g. code not exactly 6 chars) |

The code is **never** returned in API responses.

### Finding a DevelopmentLog code

1. Run the API with `--launch-profile http`.
2. Register a user.
3. In the API console, look for `[DevelopmentLog email] Verification code generated for {email} ... Code=######`.

Unverified accounts cannot sign in (`auth.accountNotVerified`).

## Password reset

Identity reset tokens. Forgot-password always returns a generic success message. Development logs reset tokens to the API console.

## Roles and policies

| Role | Policy access |
|------|----------------|
| Parent | `ParentOnly` (`/api/parent/**`, portal probe `/api/portal/parent`) |
| SchoolOwner, SchoolAdmin | `SchoolPortal` |
| PlatformAdmin | `PlatformAdminOnly` |
| SupportAgent, PlatformAdmin | `SupportOrAdmin` |

Parent profile and child APIs: see [parent-dashboard.md](./parent-dashboard.md). Non-Parent roles receive `403` on `/api/parent/**`.

Test probes: `GET /api/portal/{parent|school|admin|support}`.

## Rate limiting

Per-IP fixed windows on auth POST endpoints (`auth-register`, `auth-login`, etc.). Returns `429` with `auth.rateLimited`.

## Stable error codes

See `Schoolera.Application.Auth.Constants.AuthErrorCodes`. Angular maps `errorCodes`, never localized `errors` text.

Identity failures are mapped from `IdentityError.Code` (never `Description`) via `IdentityErrorMapper`:

| Identity code | Schoolera code |
|---------------|----------------|
| `PasswordRequiresNonAlphanumeric` | `auth.passwordRequiresNonAlphanumeric` |
| `PasswordRequiresLower` | `auth.passwordRequiresLowercase` |
| `PasswordRequiresUpper` | `auth.passwordRequiresUppercase` |
| `PasswordRequiresDigit` | `auth.passwordRequiresDigit` |
| `PasswordTooShort` | `auth.passwordTooShort` |
| `DuplicateEmail` / `DuplicateUserName` | `auth.emailAlreadyExists` |
| `InvalidEmail` | `auth.invalidEmail` |

API `errors` remain localized (`AuthMessages` resx). The Angular SPA translates the matching Transloco keys under `auth.errors.*`.

## Angular validation UX

- `extractApiFailure` / `resolveAuthFailureCodes` read `errorCodes` from HTTP 4xx bodies.
- Registration and reset forms use `se-form-error-summary`, field-level `server` errors, and `ToastService` (`se-toast-host`).
- Do not display raw ASP.NET Identity English descriptions in the UI.

## Development seed users

When `Database:SeedData=true` and `Auth:SeedUsers:DefaultPassword` is set:

| Email | Role | Password (Development default) |
|-------|------|--------------------------------|
| parent@schoolera.local | Parent | `Schoolera@Dev1` |
| schoolowner@schoolera.local | SchoolOwner | `Schoolera@Dev1` |
| schooladmin@schoolera.local | SchoolAdmin | `Schoolera@Dev1` |
| admin@schoolera.local | PlatformAdmin | `Schoolera@Dev1` |
| support@schoolera.local | SupportAgent | `Schoolera@Dev1` |

School portal seed also creates an active `SchoolTeamMember` linking `schooladmin@schoolera.local` to a demo school when catalog seed data is present (see `docs/school-portal.md`).

Idempotent by normalized email. Override password via user secrets / environment variables — never commit production credentials.

Platform Admin UI and APIs: see `docs/platform-admin.md`.

## Angular

- `AuthApi` → NSwag `Client` (JSON POST bodies)
- `AuthService` — signals, `initSession()` on app startup
- Guards: `authGuard`, `roleGuard`, `guestGuard`
- `sanitizeReturnUrl` — internal paths only
- Post-login destinations: `/parent`, `/school`, `/admin`, `/support`

## NSwag

Regenerate by starting the API in Development with `Nswag:Enabled=true`. Never edit `SwaggerClient.service.ts` manually.

## Security limitations

- No production email/SMS delivery
- No MFA
- No refresh-token rotation (cookie session only)
- Runtime metadata/cookies only — no SSR

## Adding protected routes later

1. Add backend policy or `[Authorize]` as needed.
2. Regenerate NSwag if new endpoints are exposed.
3. Add Angular route with `canActivate: [authGuard, roleGuard]` and `data: { roles: [...] }`.
