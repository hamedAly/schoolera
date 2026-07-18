# Parent favorites and support tickets (Phase 1)

Audit date: 2026-07-17  
Feature: Parent School Favorites + admission-linked Support Tickets

## Favorites

- Ownership key: `ParentUserId` (`ApplicationUser.Id`), consistent with notifications/subscriptions.
- Unique `(ParentUserId, SchoolId)`.
- Add/remove are idempotent.
- Stored favorites survive School unpublish/suspend; Parent list returns safe unavailable summaries without restricted profile data.
- Apply uses existing admission eligibility; favoriting does not grant eligibility.

## Support tickets

Statuses: Open, InProgress, WaitingForCustomer, Resolved, Closed (Closed terminal).  
Priorities: Low, Normal, High, Urgent.  
Categories: GeneralSupport, Account, SchoolInformation, AdmissionApplication, Documents, TechnicalIssue, Other.

Messages: append-only; Parent = CustomerVisible only; Support may add InternalSupportNote.

## SLA (timestamps + overdue only)

**Product SLA durations are unresolved** in authoritative docs.

Phase 1 uses `SupportTickets:Sla` configuration as a **Development / non-production foundation**.
Do not treat seeded defaults as approved production SLA values.

WaitingForCustomer does **not** pause SLA clocks in Phase 1 (no approved pause rule).

No automated escalation, paging, or reassignment jobs.

## ContactRequest conversion

Optional PlatformAdmin action: convert ContactRequest → SupportTicket when the request email
matches an existing Parent user. Idempotent via `SourceContactRequestId`. Models remain separate.

## Notifications

Uses Prompt 10 `INotificationOutboxPublisher` for Parent-visible ticket events (no message bodies,
no internal notes).
