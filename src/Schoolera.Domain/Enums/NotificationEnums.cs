namespace Schoolera.Domain.Enums;

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Sms = 3,
    WhatsApp = 4,
}

public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5,
    DeadLetter = 6,
    Skipped = 7,
    Cancelled = 8,
}

/// <summary>Server-controlled notification event types. Never accept arbitrary client values.</summary>
public enum NotificationEventType
{
    AccountVerification = 1,
    RegistrationConfirmation = 2,
    AdmissionApplicationSubmitted = 10,
    AdmissionStatusChanged = 11,
    MissingInformationRequested = 12,
    MissingInformationResubmitted = 13,
    InterviewScheduled = 20,
    InterviewRescheduled = 21,
    InterviewCancelled = 22,
    InterviewRescheduleRequired = 23,
    AssessmentScheduled = 30,
    AssessmentRescheduled = 31,
    AssessmentCancelled = 32,
    AssessmentRescheduleRequired = 33,
    WaitingList = 40,
    Accepted = 41,
    Rejected = 42,
    Registered = 43,
    AdmissionsOpened = 50,
    SupportTicketCreated = 60,
    SupportTicketReply = 61,
    SupportTicketWaitingForCustomer = 62,
    SupportTicketResolved = 63,
    SupportTicketClosed = 64,
    SupportTicketReopened = 65,
    PaymentInitiated = 70,
    PaymentPending = 71,
    PaymentSucceeded = 72,
    PaymentFailed = 73,
    PaymentCancelled = 74,
    PaymentRefundCompleted = 75,
    FinancingOffersAvailable = 80,
    FinancingApproved = 81,
    FinancingDeclined = 82,
    FinancingOfferExpired = 83,
    FinancingFunded = 84,
    AppointmentProposed = 90,
    AppointmentConfirmed = 91,
    AppointmentChanged = 92,
    AppointmentCancelled = 93,
    ParentAppointmentConfirmed = 94,
    ParentAlternateSlotSelected = 95,
    ParentAppointmentRescheduleRequested = 96,
    ParentAppointmentCancelled = 97,
    AppointmentCompleted = 98,
    AppointmentNoShow = 99,
    OnlineMeetingReady = 100,
    OnlineMeetingProvisioningFailed = 101,
    OnlineMeetingCancelled = 102,
    CourierHealthIncident = 110,
}

public enum NotificationEventCategory
{
    TransactionalSecurity = 1,
    Optional = 2,
}

public static class NotificationEventClassification
{
    public static NotificationEventCategory GetCategory(NotificationEventType eventType) =>
        eventType switch
        {
            NotificationEventType.AdmissionsOpened => NotificationEventCategory.Optional,
            _ => NotificationEventCategory.TransactionalSecurity,
        };

    public static bool IsMandatory(NotificationEventType eventType) =>
        GetCategory(eventType) == NotificationEventCategory.TransactionalSecurity;
}
