using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Privacy-safe, idempotent timeline entry for an appointment mutation.</summary>
public sealed class AdmissionAppointmentActionHistory
{
    private AdmissionAppointmentActionHistory() { }

    public AdmissionAppointmentActionHistory(
        Guid admissionApplicationId,
        Guid appointmentId,
        SlotKind kind,
        AdmissionAppointmentAction action,
        AdmissionAppointmentLifecycle previousLifecycle,
        AdmissionAppointmentLifecycle newLifecycle,
        Guid? oldSlotId,
        Guid? newSlotId,
        AdmissionAppointmentActorType actorType,
        Guid? actorUserId,
        string? safeReason,
        string? idempotencyKey)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        AppointmentId = appointmentId;
        Kind = kind;
        Action = action;
        PreviousLifecycle = previousLifecycle;
        NewLifecycle = newLifecycle;
        OldSlotId = oldSlotId;
        NewSlotId = newSlotId;
        ActorType = actorType;
        ActorUserId = actorUserId;
        SafeReason = Normalize(safeReason, 500);
        IdempotencyKey = Normalize(idempotencyKey, 128);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public SlotKind Kind { get; private set; }
    public AdmissionAppointmentAction Action { get; private set; }
    public AdmissionAppointmentLifecycle PreviousLifecycle { get; private set; }
    public AdmissionAppointmentLifecycle NewLifecycle { get; private set; }
    public Guid? OldSlotId { get; private set; }
    public Guid? NewSlotId { get; private set; }
    public AdmissionAppointmentActorType ActorType { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? SafeReason { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var plain = value.Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return plain[..Math.Min(max, plain.Length)];
    }
}
