using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Application-owned interview appointment (cancelling does not cancel the application).</summary>
public sealed class AdmissionInterviewAppointment
{
    private AdmissionInterviewAppointment()
    {
    }

    public AdmissionInterviewAppointment(
        Guid admissionApplicationId,
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        ApplySchedule(
            scheduledAtUtc,
            timeZoneId,
            mode,
            location,
            onlineInstructions,
            parentVisibleNotes,
            preparationInstructions);
        Lifecycle = AdmissionAppointmentLifecycle.Proposed;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ProposedAtUtc = CreatedAtUtc;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public Guid? InterviewAssessmentSlotId { get; private set; }

    public InterviewAssessmentSlot? InterviewAssessmentSlot { get; private set; }

    public DateTimeOffset ScheduledAtUtc { get; private set; }

    public string TimeZoneId { get; private set; } = null!;

    public AdmissionAppointmentMode Mode { get; private set; }

    public string? Location { get; private set; }

    public string? OnlineInstructions { get; private set; }

    public string? ParentVisibleNotes { get; private set; }

    public string? PreparationInstructions { get; private set; }

    public AdmissionAppointmentLifecycle Lifecycle { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public string? OutcomeNotes { get; private set; }

    public DateTimeOffset ProposedAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public DateTimeOffset? RescheduleRequestedAtUtc { get; private set; }

    public DateTimeOffset? NoShowAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public int ParentRescheduleAttemptCount { get; private set; }

    public string? LastParentVisibleRescheduleReason { get; private set; }

    public string? CancellationReason { get; private set; }

    public AppointmentRescheduleInitiator? RescheduleInitiator { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public bool IsActiveReservation =>
        Lifecycle is AdmissionAppointmentLifecycle.Proposed or AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.InProgress;

    public void LinkToSlot(Guid slotId) => InterviewAssessmentSlotId = slotId;

    public void Propose(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid? slotId,
        Guid actorUserId)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
        {
            throw new InvalidOperationException("The interview cannot be proposed in its current state.");
        }

        ApplySchedule(
            scheduledAtUtc,
            timeZoneId,
            mode,
            location,
            onlineInstructions,
            parentVisibleNotes,
            preparationInstructions);
        if (slotId.HasValue) InterviewAssessmentSlotId = slotId;
        Lifecycle = AdmissionAppointmentLifecycle.Proposed;
        ProposedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        ConfirmedAtUtc = null;
        RescheduleRequestedAtUtc = null;
        RescheduleInitiator = null;
    }

    public void ConfirmCurrent()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Proposed);
        Lifecycle = AdmissionAppointmentLifecycle.Confirmed;
        ConfirmedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SelectAlternate(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid slotId)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
            throw new InvalidOperationException("An alternate slot cannot be selected.");

        var consumesAttempt =
            Lifecycle is AdmissionAppointmentLifecycle.Proposed or AdmissionAppointmentLifecycle.Confirmed &&
            InterviewAssessmentSlotId != slotId;
        ApplySchedule(scheduledAtUtc, timeZoneId, mode, location, onlineInstructions,
            parentVisibleNotes, preparationInstructions);
        InterviewAssessmentSlotId = slotId;
        if (consumesAttempt) ParentRescheduleAttemptCount++;
        Lifecycle = AdmissionAppointmentLifecycle.Confirmed;
        ConfirmedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        RescheduleRequestedAtUtc = null;
        RescheduleInitiator = null;
    }

    public void RequestReschedule(string? reason, AppointmentRescheduleInitiator initiator)
    {
        if (Lifecycle == AdmissionAppointmentLifecycle.RescheduleRequested &&
            RescheduleInitiator == initiator) return;
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed))
            throw new InvalidOperationException("A reschedule cannot be requested.");

        Lifecycle = AdmissionAppointmentLifecycle.RescheduleRequested;
        RescheduleInitiator = initiator;
        RescheduleRequestedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        LastParentVisibleRescheduleReason = NormalizeReason(reason);
        if (initiator == AppointmentRescheduleInitiator.Parent)
            ParentRescheduleAttemptCount++;
    }

    public void CancelByParent(string? reason) => CancelCore(reason);

    public void CancelBySchool(string? reason) => CancelCore(reason);

    private void CancelCore(string? reason)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
            throw new InvalidOperationException("The interview cannot be cancelled.");
        Lifecycle = AdmissionAppointmentLifecycle.Cancelled;
        CancellationReason = NormalizeReason(reason);
        CancelledAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CancelledAtUtc.Value;
    }

    public void StartSession()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Confirmed);
        Lifecycle = AdmissionAppointmentLifecycle.InProgress;
        StartedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Complete(string? outcomeNotes)
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.InProgress);
        Lifecycle = AdmissionAppointmentLifecycle.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        OutcomeNotes = NormalizeOptional(outcomeNotes);
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkNoShow()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Confirmed);
        Lifecycle = AdmissionAppointmentLifecycle.NoShow;
        NoShowAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureLifecycle(AdmissionAppointmentLifecycle expected)
    {
        if (Lifecycle != expected)
            throw new InvalidOperationException("Invalid appointment transition.");
    }

    private static string? NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var plain = value.Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return plain[..Math.Min(500, plain.Length)];
    }

    private void ApplySchedule(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        ScheduledAtUtc = scheduledAtUtc;
        TimeZoneId = NormalizeRequired(timeZoneId, 64);
        Mode = mode;
        Location = mode == AdmissionAppointmentMode.InPerson
            ? NormalizeOptional(location)
            : null;
        OnlineInstructions = mode == AdmissionAppointmentMode.Online
            ? NormalizeOptional(onlineInstructions)
            : null;
        ParentVisibleNotes = NormalizeOptional(parentVisibleNotes);
        PreparationInstructions = NormalizeOptional(preparationInstructions);
    }

    private static string NormalizeRequired(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= FieldLengthLimits.AdmissionHistoryNote
            ? trimmed
            : trimmed[..FieldLengthLimits.AdmissionHistoryNote];
    }
}

/// <summary>Application-owned assessment appointment (cancelling does not cancel the application).</summary>
public sealed class AdmissionAssessmentAppointment
{
    private AdmissionAssessmentAppointment()
    {
    }

    public AdmissionAssessmentAppointment(
        Guid admissionApplicationId,
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        ApplySchedule(
            scheduledAtUtc,
            timeZoneId,
            mode,
            location,
            onlineInstructions,
            parentVisibleNotes,
            preparationInstructions);
        Lifecycle = AdmissionAppointmentLifecycle.Proposed;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ProposedAtUtc = CreatedAtUtc;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public Guid? InterviewAssessmentSlotId { get; private set; }

    public InterviewAssessmentSlot? InterviewAssessmentSlot { get; private set; }

    public DateTimeOffset ScheduledAtUtc { get; private set; }

    public string TimeZoneId { get; private set; } = null!;

    public AdmissionAppointmentMode Mode { get; private set; }

    public string? Location { get; private set; }

    public string? OnlineInstructions { get; private set; }

    public string? ParentVisibleNotes { get; private set; }

    public string? PreparationInstructions { get; private set; }

    public AdmissionAppointmentLifecycle Lifecycle { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public string? OutcomeNotes { get; private set; }

    public DateTimeOffset ProposedAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public DateTimeOffset? RescheduleRequestedAtUtc { get; private set; }

    public DateTimeOffset? NoShowAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public int ParentRescheduleAttemptCount { get; private set; }

    public string? LastParentVisibleRescheduleReason { get; private set; }

    public string? CancellationReason { get; private set; }

    public AppointmentRescheduleInitiator? RescheduleInitiator { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public bool IsActiveReservation =>
        Lifecycle is AdmissionAppointmentLifecycle.Proposed or AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.InProgress;

    public void LinkToSlot(Guid slotId) => InterviewAssessmentSlotId = slotId;

    public void Propose(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid? slotId,
        Guid actorUserId)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
        {
            throw new InvalidOperationException("The assessment cannot be proposed in its current state.");
        }

        ApplySchedule(
            scheduledAtUtc,
            timeZoneId,
            mode,
            location,
            onlineInstructions,
            parentVisibleNotes,
            preparationInstructions);
        if (slotId.HasValue) InterviewAssessmentSlotId = slotId;
        Lifecycle = AdmissionAppointmentLifecycle.Proposed;
        ProposedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        ConfirmedAtUtc = null;
        RescheduleRequestedAtUtc = null;
        RescheduleInitiator = null;
    }

    public void ConfirmCurrent()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Proposed);
        Lifecycle = AdmissionAppointmentLifecycle.Confirmed;
        ConfirmedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SelectAlternate(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions,
        Guid slotId)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
            throw new InvalidOperationException("An alternate slot cannot be selected.");

        var consumesAttempt =
            Lifecycle is AdmissionAppointmentLifecycle.Proposed or AdmissionAppointmentLifecycle.Confirmed &&
            InterviewAssessmentSlotId != slotId;
        ApplySchedule(scheduledAtUtc, timeZoneId, mode, location, onlineInstructions,
            parentVisibleNotes, preparationInstructions);
        InterviewAssessmentSlotId = slotId;
        if (consumesAttempt) ParentRescheduleAttemptCount++;
        Lifecycle = AdmissionAppointmentLifecycle.Confirmed;
        ConfirmedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        RescheduleRequestedAtUtc = null;
        RescheduleInitiator = null;
    }

    public void RequestReschedule(string? reason, AppointmentRescheduleInitiator initiator)
    {
        if (Lifecycle == AdmissionAppointmentLifecycle.RescheduleRequested &&
            RescheduleInitiator == initiator) return;
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed))
            throw new InvalidOperationException("A reschedule cannot be requested.");

        Lifecycle = AdmissionAppointmentLifecycle.RescheduleRequested;
        RescheduleInitiator = initiator;
        RescheduleRequestedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        LastParentVisibleRescheduleReason = NormalizeReason(reason);
        if (initiator == AppointmentRescheduleInitiator.Parent)
            ParentRescheduleAttemptCount++;
    }

    public void CancelByParent(string? reason) => CancelCore(reason);

    public void CancelBySchool(string? reason) => CancelCore(reason);

    private void CancelCore(string? reason)
    {
        if (Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.RescheduleRequested))
            throw new InvalidOperationException("The assessment cannot be cancelled.");
        Lifecycle = AdmissionAppointmentLifecycle.Cancelled;
        CancellationReason = NormalizeReason(reason);
        CancelledAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CancelledAtUtc.Value;
    }

    public void StartSession()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Confirmed);
        Lifecycle = AdmissionAppointmentLifecycle.InProgress;
        StartedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Complete(string? outcomeNotes)
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.InProgress);
        Lifecycle = AdmissionAppointmentLifecycle.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        OutcomeNotes = NormalizeOptional(outcomeNotes);
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkNoShow()
    {
        EnsureLifecycle(AdmissionAppointmentLifecycle.Confirmed);
        Lifecycle = AdmissionAppointmentLifecycle.NoShow;
        NoShowAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureLifecycle(AdmissionAppointmentLifecycle expected)
    {
        if (Lifecycle != expected)
            throw new InvalidOperationException("Invalid appointment transition.");
    }

    private static string? NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var plain = value.Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return plain[..Math.Min(500, plain.Length)];
    }

    private void ApplySchedule(
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        AdmissionAppointmentMode mode,
        string? location,
        string? onlineInstructions,
        string? parentVisibleNotes,
        string? preparationInstructions)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        ScheduledAtUtc = scheduledAtUtc;
        TimeZoneId = NormalizeRequired(timeZoneId, 64);
        Mode = mode;
        Location = mode == AdmissionAppointmentMode.InPerson
            ? NormalizeOptional(location)
            : null;
        OnlineInstructions = mode == AdmissionAppointmentMode.Online
            ? NormalizeOptional(onlineInstructions)
            : null;
        ParentVisibleNotes = NormalizeOptional(parentVisibleNotes);
        PreparationInstructions = NormalizeOptional(preparationInstructions);
    }

    private static string NormalizeRequired(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= FieldLengthLimits.AdmissionHistoryNote
            ? trimmed
            : trimmed[..FieldLengthLimits.AdmissionHistoryNote];
    }
}
