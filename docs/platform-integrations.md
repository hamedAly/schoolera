# Platform integrations and notifications (Phase 1)

Audit date: 2026-07-17  
Feature: Database-managed integration configuration, notification outbox, Parent InApp, admissions-open subscriptions

## Approved architecture decision

**All external integration provider credentials and settings are stored in the database**
through `PlatformIntegrationConfiguration.SettingsJson`, managed only by Platform Admin.

| Must stay in DB | Must NOT use for provider settings |
|-----------------|--------------------------------------|
| API keys, tokens, sender IDs, endpoints, webhook secrets, provider options | `appsettings`, User Secrets, environment variables, hardcoded constants |

Only unavoidable bootstrap configuration remains outside the database (connection string, environment name).

Worker **operational** settings (batch size, poll interval, max attempts) may use
`Notifications:Worker` appsettings. They are not provider credentials.

## Phase 1 security limitation (plaintext)

Provider `SettingsJson` is stored as **plaintext JSON** in SQL Server for Phase 1.

- This is **not** encryption-at-rest and is **not** claimed to be secure storage.
- Encryption-at-rest is an explicit future hardening task.
- The configuration access abstraction is designed so encryption can be added later
  without changing notification providers or Admin workflows.
- Application logs, audit summaries, health endpoints, notification records, and
  ordinary API responses must **never** include full `SettingsJson` or secret properties.
  Use centralized redaction / masked Admin edit behavior.

## Operational scope

Implemented: Email, SMS, WhatsApp **configuration + simulated/development providers**,
InApp channel, outbox worker, templates, Parent preferences/consent, admissions-open
subscriptions, Platform Admin Integrations + Templates UI,
and **Map** integration type for School Search (Leaflet client + DB tile settings; Production tile provider Product-blocked — see `docs/map-based-school-search.md`).

Not implemented in Prompt 10: courier delivery, school transfers, personal WhatsApp
automation, message bus, notification microservice, SettingsJson encryption,
bulk marketing, payment notifications, parent–school chat.

## Meeting delivery foundation

`IntegrationType.Meeting` reuses `PlatformIntegrationConfiguration.SettingsJson`.
Phase 1 Meeting settings and any future provider credentials therefore remain plaintext in SQL
Server under the same known limitation above; complete settings and secrets are never returned to
Parent/School APIs, operational DTOs, history, notifications, or logs.

The only implemented provider is `Simulated`, available in Development/Test and blocked from
Production activation. It performs no external network or media operation. Confirmation of an
Online appointment creates one pending `AdmissionMeetingSession`; `MeetingSessionWorker` is the
single provisioning path. The session preserves its Integration configuration ID, provider code,
environment, schema version, configuration row version, schedule, and generation, so changing the
current default does not retarget an existing session.

Disabling the referenced Integration blocks new provisioning and Parent/Host access. It does not
move or change an Appointment; the historical configuration may still be used for idempotent
provider cancellation so an already-created external session can be cleaned up. An unhealthy
Integration is also excluded from new provisioning and access. Appointment
cancellation/rescheduling makes access unavailable immediately and requests idempotent provider
cancellation where supported. Provider failure never
silently changes Online to OnSite; Hybrid fallback continues through the explicit existing
appointment replacement/rescheduling workflow.

## Callbacks

Real provider HTTP callbacks are **not applicable** until a production provider with a
known signature contract is configured. Simulated providers do not expose callbacks.
Do not invent fake callback verification.

## Courier foundation (Prompt v1.4-08)

Courier uses the existing `PlatformIntegrationConfiguration` row for provider identity,
activation/default selection, health status, and provider transport settings. Provider credentials
and transport values (`ApiBaseUrl`, account identifiers, keys/tokens/secrets, timeout, retry, and
simulated scenarios) remain in plaintext `SettingsJson` under the limitation above. Normalized
Courier tables contain only queryable product metadata: bilingual provider profile, services,
coverage rules, operating windows, elapsed-clock SLA definitions, and health-check history.
Secrets and complete settings are never returned by Courier public/admin DTOs or notifications.

Coverage resolution is deterministic:

1. Match the requested location hierarchy and service.
2. Prefer the most specific geography: District, then City, Governorate, Country.
3. At the same geography, prefer a service-specific rule over a provider-wide rule.
4. Use row ID only as the final deterministic tie-breaker.
5. For operating windows, prefer the selected coverage rule, then service-specific over generic,
   then the most specific available geography scope.
6. For SLA, prefer selected-coverage + service, selected-coverage generic, service-wide, then
   provider-wide.

`District` is the Courier `Area` concept; no second Area taxonomy is introduced. SLA minute values
are elapsed-clock targets, not business-minute calculations. Availability requires an active
default configured provider, a current Healthy check within `Courier:HealthFreshnessMinutes`, an
available provider response, covered geography, an operating window, and an active service.
Degraded, Unhealthy, missing, or stale health returns no options.

Repeated Degraded/Unhealthy checks alert active Platform Admin users through bilingual InApp
notifications when `Courier:HealthFailureAlertThreshold` is first reached (default 3). The incident
is deduplicated for subsequent failures and resets only after a Healthy check. Alerts contain only
the provider display name/code, health status, and provider-safe code, and link to
`/admin/integrations`.

Runtime execution is deliberately simulated-only and Development/Test-only. The foundation can
validate and store future Sandbox/Production-shaped settings, but no real courier network adapter,
pickup creation, shipment entity, callback, polling job, or external HTTP call is implemented.
Checking availability is read-only and never creates a shipment or pickup.

Unresolved dependencies:

- `DropOffPoint` needs a product model, location source, and provider contract; it is rejected and
  reported unsupported today.
- `InternalDelivery` needs a school-owned workflow, custody/status model, permissions, and audit
  requirements; it is not represented by the provider Courier flow.

Explicit exclusions: real provider onboarding, encrypted credential storage, pickup/shipment
creation, labels or tracking, courier assignment, proof of pickup/delivery, webhooks, polling,
pricing/payment, claims/refunds, DropOffPoint, and InternalDelivery.

Stable Courier result codes currently used by handlers are documented for client branching:
`courier.forbidden`, `courier.notFound`, `courier.integrationTypeRequired`,
`courier.configurationConflict`, `courier.invalidLocation`, `courier.invalidTimeZone`,
`courier.invalidWindow`, `courier.overlappingWindow`, `courier.duplicateActiveScope`,
`courier.invalidOwnership`, `courier.invalidSla`, `courier.applicationCancelled`,
`courier.destinationBranchInactive`, `courier.invalidRequest`, `courier.invalidScope`, and
`courier.dropOffPointUnsupported` (plus the
`integrations.courier.*` validation family). Handler messages are currently invariant text rather
than `IStringLocalizer` resources; clients must branch only on these stable codes.

## Admission-open triggering

Automatic notifications fire when an existing `SchoolStageOffering.IsAdmissionOpen`
transitions from `false` → `true` (same offering model; no competing offering entity).
