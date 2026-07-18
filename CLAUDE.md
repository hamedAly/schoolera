@docs/implementation-baseline.md
@docs/localization.md

# Schoolera — Claude Code instructions

## Scope and honesty

- Inspect existing code before changing it.
- Implement only the requested prompt scope, then stop.
- Run relevant builds and tests; never claim a command succeeded unless it ran.

## Architecture

- Preserve Clean Architecture: `Schoolera.Api`, `Application`, `Domain`, `Infrastructure`, `Tests`.
- Thin controllers + MediatR; explicit DTOs; never expose EF entities.
- Preserve `Result<T>` (`succeeded` / `data` / `errors` / `errorCodes`).
- Preserve migrations and data; avoid destructive schema changes unless explicitly required.
- Do not invent parallel architectures, generic-repository stacks, microservices, or event buses.

## NSwag (official Angular API contract)

- NSwag is the official frontend API contract.
- Generated path: `src/Schoolera.Api/Schoolera-SPA/src/app/core/api-client/SwaggerClient.service.ts`
- Never edit generated source manually.
- Change backend DTOs/OpenAPI first, regenerate the client (Development API startup when `Nswag:Enabled`), then update Angular consumers.
- Feature services wrap generated clients; components should not call generated clients directly.
- Do not duplicate generated DTOs; UI-only view models are allowed when clearly separate.

## Hosting

- Preserve ASP.NET Core SPA static hosting and same-origin `/api` production calls.
- Do not hardcode localhost into production or generated client output.
- Keep `MapControllers()` before `MapFallbackToFile("index.html")`.
- `/api/**`, `/swagger/**`, and `/uploads/**` must not fall through to Angular `index.html`.
- Use the global Accept-Language interceptor only.

## Angular

- Prefer existing feature-service patterns and shared UI reuse rules from the baseline.
- Create shared components only when reused or part of the design system.

## Localization (permanent)

- Arabic (`ar` / `ar-EG` / RTL default) and English (`en` / `en-US` / LTR) are required for Phase 1.
- Use Transloco only; never introduce ngx-translate or Angular compile-time i18n as a second competing system.
- Do not hardcode user-facing AR/EN text in templates/components; use `public/i18n` keys.
- Do not build separate Arabic/English component trees.
- Static UI → Transloco; managed public content → explicit `*Ar`/`*En` fields; management forms show both.
- Branch on `errorCodes`, not localized message text.
- Follow `docs/localization.md`. Repository rules override future prompt wording that describes English as optional.
- Run `npm run i18n:check` (and validate) before considering localization work complete.

## Authentication (Phase 1)

- Cookie-based ASP.NET Core Identity; never store auth tokens in `localStorage` or `sessionStorage`.
- Public registration cannot create privileged roles (`SchoolAdmin`, `PlatformAdmin`, `SupportAgent`).
- Authorization policies are centralized (`SchooleraPolicies`); avoid scattered role-string checks.
- State-changing cookie-authenticated API requests require CSRF protection.
- See `docs/authentication.md`.
