# Interview and assessment slots

School-owned `InterviewAssessmentSlot` records are explicit inventory. Existing
`AdmissionInterviewAppointment` and `AdmissionAssessmentAppointment` rows remain the reservation
records and may link to a slot. Parents can only list available slots; all booking and lifecycle
actions remain school operations.

## Lifecycle

| From | Open | Close | Reopen | Cancel |
|---|---:|---:|---:|---:|
| Draft | yes | no | n/a | yes |
| Open | no | yes | n/a | yes |
| Closed | n/a | n/a | yes | yes |
| Cancelled | no | no | no | idempotent |

Cancelled is terminal. Open inventory must satisfy the currently published applicable policy,
active branch and scope, exact policy duration, lead time, capacity, bilingual instructions,
delivery mode, resource and meeting-provider boundary.

`BookingWindowOpensDaysBefore` and `BookingWindowClosesDaysBefore` are interpreted relative to
`AcademicYear.StartDate` at 00:00 UTC: a slot may not start before start-date minus opens-days and
may not start after start-date minus closes-days when each value is present.

## Time and conflicts

Manual create/edit and recurrence submit local date/time values plus an OS-supported
`TimeZoneInfo` identifier. The server centrally converts both interval boundaries to UTC.
Invalid and ambiguous DST local times reject the entire request; callers must choose a different
unambiguous local time. Manual Phase 1 intervals must start and end on the same local date.

Intervals are half-open. They overlap exactly when
`existing.StartAtUtc < new.EndAtUtc && existing.EndAtUtc > new.StartAtUtc`; adjacent slots are
allowed. `AvailableSeats` is not stored. It is capacity minus linked appointments whose lifecycle
is `Proposed` or `Confirmed`.

## Recurrence

Recurrence creates explicit Draft rows only. Daily and weekly-selected-weekday generation is
bounded to 180 calendar days and 100 occurrences. There is no RRULE support. A school-scoped
request key is limited to 128 characters and is paired with a SHA-256 request fingerprint.
Same-key/same-fingerprint returns the existing batch; a different fingerprint is rejected.
Preview writes nothing. Generate validates scope/resource first, then performs idempotency lookup,
Open-resource conflict checks, batch/slot/audit inserts, and save in one serializable transaction.
Draft inventory does not conflict, and resource-free recurrence may overlap.

## Cancellation

Cancellation requires Arabic and English reasons. Linked `Proposed` or `Confirmed` appointments
become `RescheduleRequested` with `School` as the initiator while preserving their slot foreign key
for history. Application status is unchanged. One parent-visible application history row and a
deduplicated notification are added per affected appointment. No replacement is assigned.

## Security and boundaries

Slot writes require CSRF, `ManageAdmissionRequirements`, an editable school and `RequireBranch`.
Appointment scheduling requires `ManageApplicationReview`, an editable school and branch scope.
Controlled resources are limited to active same-school `SchoolTeamMember` memberships.

Online opening checks that the safe provider code matches policy and that an active Meeting
integration exists. Provider settings and `SettingsJson` are never returned. Parent DTOs omit
staff/resource/provider/configuration and all other reservations.

## Concurrency and locking

Final-seat assignment runs through the EF execution strategy in a serializable SQL transaction.
It locks and revalidates the Open slot row, counts `Proposed` and `Confirmed` interview and
assessment appointments (excluding the appointment being rescheduled), performs all application,
appointment, history, and outbox mutations, saves, and commits while the lock is held.

Opening and reopening exclusive-resource slots acquires a transaction-scoped `sp_getapplock`
keyed by school/resource kind/resource ID and rechecks half-open overlaps against Open slots before
the transition is saved. Resource-free slots do not acquire an exclusive resource lock and may
overlap; adjacent intervals remain valid. Filtered unique indexes additionally permit at most one
active (`Proposed` or `Confirmed`) interview and one active assessment per application.
