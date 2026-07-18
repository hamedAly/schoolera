# Admission lifecycle (statuses, missing information, appointments)

Extends [admission-applications.md](./admission-applications.md) with Phase 1 post-submit lifecycle statuses. Requirements and question snapshots remain prerequisites for submit; this document covers statuses after `Submitted`.

## Statuses

| Value | Name |
|------:|------|
| 1–6 | Draft, Submitted, UnderReview, Accepted, Rejected, Cancelled (unchanged) |
| 7 | MissingDocuments |
| 8 | InterviewRequired |
| 9 | AssessmentRequired |
| 10 | WaitingList |
| 11 | Registered |

**Intentionally absent:** `ContractSent`, `Paid`.

## Transition matrix

Central policy: `AdmissionTransitionPolicy` (do not duplicate in controllers or Angular).

### Parent

| From → To | Notes |
|-----------|--------|
| Draft → Submitted | Existing |
| Draft/Submitted → Cancelled | Existing cancellation policy (Submitted only before review starts) |
| MissingDocuments → UnderReview | Resubmit after completing requested items |
| UnderReview / Interview / Assessment / WaitingList → Cancelled | Listed only when existing `CanParentCancel` allows — currently **blocked** |

### School

| From → To |
|-----------|
| Submitted → UnderReview |
| UnderReview → MissingDocuments / InterviewRequired / AssessmentRequired / WaitingList / Accepted / Rejected |
| InterviewRequired → UnderReview / WaitingList / Accepted / Rejected |
| AssessmentRequired → UnderReview / WaitingList / Accepted / Rejected |
| WaitingList → UnderReview / Accepted / Rejected |
| Accepted → Registered |

Legacy School cancellation of an interview/assessment appointment returns the application to
`UnderReview`. Parent appointment cancellation preserves `InterviewRequired` or
`AssessmentRequired`. Neither action sets the application status to `Cancelled`.

## Missing documents

School request references application-owned snapshots/fields only (`RequirementSnapshot`, `QuestionSnapshot`, `ParentSnapshotField`, `ChildSnapshotField`). Parent may edit only requested items; resubmit clears the active request and moves to `UnderReview`.

## Appointments

`AdmissionInterviewAppointment` and `AdmissionAssessmentAppointment` store schedule, IANA/Windows
timezone id, Online/InPerson mode, parent-visible notes, policy-bounded Parent attempt counts, and
the lifecycle `Proposed`, `Confirmed`, `RescheduleRequested`, `Cancelled`, `Completed`, or
`NoShow`. `AdmissionAppointmentActionHistory` records safe append-only transitions and idempotency.
No meeting secrets, join tokens, or permanent URLs are stored in DTOs, logs, or history.

## Waiting list / Registered

Waiting list stores parent-visible reason, optional position (never fabricated), optional review date. Registered means school confirmed registration in-platform only — not payment or contract.

## Notifications

No delivery platform. Events are recorded on application history. Email/SMS/WhatsApp remain future work.
