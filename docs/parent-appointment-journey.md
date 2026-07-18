# Parent appointment journey

Interview and assessment journeys reuse `AdmissionInterviewAppointment` and
`AdmissionAssessmentAppointment`. Attempts are stored on each appointment, so the two kinds have
independent limits. Lifecycle values 1 and 4 retain their existing database values but now mean
`Proposed` and `RescheduleRequested`.

## Transition matrix

| Current | Action | Actor | Next | Parent attempt |
|---|---|---|---|---|
| none / RescheduleRequested | propose | School | Proposed | 0 |
| Proposed | replace proposal | School | Proposed | 0 |
| Proposed | confirm current | Parent | Confirmed | 0 |
| Proposed / Confirmed | select a different open slot | Parent | Confirmed | +1 |
| RescheduleRequested | select an open slot | Parent | Confirmed | 0 |
| Proposed / Confirmed | request reschedule | Parent | RescheduleRequested | +1 |
| Proposed / Confirmed | slot cancellation | School | RescheduleRequested | 0 |
| Proposed / Confirmed / RescheduleRequested | cancel | Parent | Cancelled | 0 |
| Confirmed | complete / no-show | School | Completed / NoShow | 0 |

Cancelled, Completed, and NoShow are terminal for Parent actions. A School cannot directly replace
a Confirmed appointment. Active capacity is reserved only by Proposed and Confirmed appointments;
RescheduleRequested keeps its old slot reference only as history.

## Policy and atomic movement

The application policy snapshot is authoritative. Parent rescheduling and cancellation use the
current slot start minus `MinimumSchedulingLeadTimeHours` as the deadline. Parent-initiated actions
enforce `MaxParentRescheduleAttempts`. Recovery from a School-induced slot cancellation may select
another eligible slot even when Parent rescheduling is disabled and consumes no attempt.

Parent writes execute under the SQL Server retry strategy and a Serializable transaction. The
application/appointment and relevant slots are locked before validation. Destination eligibility
and capacity are checked before changing the appointment, so a full or invalid destination leaves
the source reservation unchanged. Idempotency is enforced by the unique
`(AppointmentId, IdempotencyKey)` action-history index.

Confirming a proposal revalidates the current appointment row version and its linked slot while
those rows are locked. The slot must still exist, match the appointment's immutable schedule and
the application's School, Branch, stage, grade, academic year, kind, and delivery mode, and must
not have started. A Closed slot remains valid for an already-reserved proposal; Draft and Cancelled
slots do not. Failed confirmation creates no timeline, application history, notification, or
attempt.

## Privacy, join, and support

`AdmissionAppointmentActionHistory` contains only bounded, plain-text safe reasons and identifiers;
it never stores credentials, meeting URLs, provider settings, or full sensitive explanations.
Parent DTOs expose capabilities and the support fallback:
`/parent/support-tickets/new?admissionApplicationId={id}`.

Online join is a server-controlled POST. It requires a Confirmed online appointment and the window
from 15 minutes before slot start through slot end. No real meeting adapter exists, so capabilities
always report join as unavailable and an otherwise valid attempt returns
`admission.appointment.joinProviderUnavailable` without a URL or token.

Legacy School manual scheduling remains available for backward compatibility. The Parent journey
and new School proposals use explicit eligible slots. A School proposal or replacement that
supplies a slot also supplies an idempotency key of at most 128 characters. The key is checked
under the same serializable transaction as capacity assignment. Repeating the same application,
kind, proposal action, and target slot returns the existing success without another timeline,
notification, or reservation; reusing the key for a different target or action returns
`admission.appointment.idempotencyConflict`. Legacy manual scheduling without a slot remains
key-optional.
