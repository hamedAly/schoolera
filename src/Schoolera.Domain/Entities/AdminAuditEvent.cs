namespace Schoolera.Domain.Entities;

/// <summary>
/// Append-only Platform Admin audit event for important administrative actions.
/// </summary>
public sealed class AdminAuditEvent
{
    public Guid Id { get; set; }

    public Guid ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Summary { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
