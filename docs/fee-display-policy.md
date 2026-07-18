# Fee display policy (Phase 1)

Audit date: 2026-07-17  
Feature: Informational fee, installment-display, discount, and fee-visibility foundation

This document records the Prompt 9 Product decision status and implementation rules.
It does **not** authorize payment collection, checkout, receipts, or accounting.

---

## 1. Egypt fee-visibility Product decision — UNRESOLVED

Authoritative search across `docs/`, `.cursor/rules`, `CLAUDE.md`, `README.md`,
`implementation-baseline.md`, `school-portal.md`, `school-catalog.md`,
`public-school-profile.md`, `admission-applications.md`, and `school-team-roles.md`
found **no** approved Egypt default choosing:

- `FeeVisibilityPolicy.Public`
- `FeeVisibilityPolicy.AuthenticatedParentsOnly`

Therefore Schoolera **must not invent** that Product decision.

### Neutral configuration foundation

| Item | Behavior |
|------|----------|
| Enum | `FeeVisibilityPolicy` (`Public = 1`, `AuthenticatedParentsOnly = 2`) |
| Storage | `Schools.FeeVisibilityPolicy` nullable `int` |
| Null meaning | Product default unresolved — **not** an implicit Public or AuthenticatedParentsOnly choice |
| Explicit Public | Anonymous and Parent responses may return approved published fee details |
| Explicit AuthenticatedParentsOnly | Anonymous responses omit amounts; may return `hasPublishedFees` / `feesRequireLogin` only |
| School Portal | Owner/FinanceOfficer/SchoolAdmin (per Prompt 8 `ManageFees`) may set an explicit policy when operating the school |

### Legacy compatibility while null

Until Product resolves the Egypt default, schools with `FeeVisibilityPolicy = null`
continue the **pre-Prompt-9** public behavior of exposing **active + published**
informational fee amounts on Search and Profile. That continuity is compatibility,
**not** an approved Egypt Product default.

---

## 2. Informational scope

Supported: fee categories, installment-display rows, published discounts,
parent-visible vs internal financial notes, portal CRUD/publish, public/Parent
visibility gating when policy is explicit.

Out of scope: payments, gateways, receipts, paid/unpaid state, balances,
accounting, binding contracts, notifications.

---

## 3. Admission application fee snapshot

**Not applicable** for Phase 1 per `admission-applications.md`
(“No application fees / payments / contracts”). No application-owned fee
snapshot entity was added.
