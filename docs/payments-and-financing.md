# Payments and consumer financing (Phase 1)

Audit date: 2026-07-17  
Feature: Provider-agnostic Payment (Pay Now) and Financing foundation — **Development Sandbox only**

## Approved configuration decision

Payment and Financing provider credentials and options are stored **only** in
`PlatformIntegrationConfiguration.SettingsJson` (Prompt 10).

| Allowed | Forbidden for provider settings |
|---------|----------------------------------|
| Database `SettingsJson` (Platform Admin) | `appsettings`, User Secrets, environment variables, hardcoded constants |

### Phase 1 plaintext limitation

Settings remain **plaintext JSON in SQL Server**. This is **not** encryption-at-rest
and is a known hardening gap. APIs, logs, audits, and reports must never emit complete
`SettingsJson` or secret properties.

## Providers in this prompt

| Provider code | Environment | Integration types | Real money? |
|---------------|-------------|-------------------|-------------|
| `SchooleraSandbox` | Sandbox | Payment, Financing | **No** |

Real Egypt bank/gateway/wallet/financing adapters are **NotConfigured** until official
documentation, credentials, and compliance approval exist. Do not invent live contracts.

## Terminology

- **Pay Now** — Parent pays a server-resolved payable amount via hosted/sandbox checkout.
- **Financing Request** — Parent requests provider offers; approval/funding is provider-controlled.
- Financing **Approved** ≠ **Funded** ≠ School settlement verified.
- Receipt is a **platform receipt**, not a tax invoice by default.
- Sandbox records are always labeled Sandbox and are never real financial transactions.

## Authoritative payable amount

Parents never supply unrestricted amounts. Backend resolves amount/currency from a
`SchoolPayableItem` (linked to a published `TuitionFee`) plus optional owned
`AdmissionApplicationId`. Client amount/currency/ownership values are ignored.

## Callbacks

Sandbox callbacks use a Development HMAC over the raw body with the integration
`WebhookSecret`. Cookie auth and CSRF are not used on callback routes.
