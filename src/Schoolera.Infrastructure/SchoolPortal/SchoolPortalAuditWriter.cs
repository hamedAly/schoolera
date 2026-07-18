using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.SchoolPortal;

/// <summary>Append-only school portal audit events (no secrets/passwords).</summary>
public sealed class SchoolPortalAuditWriter(SchooleraDbContext dbContext) : ISchoolPortalAuditWriter
{
    public async Task WriteAsync(
        Guid actorUserId,
        string action,
        string entityType,
        string? entityId,
        string? summary,
        CancellationToken cancellationToken = default)
    {
        dbContext.AdminAuditEvents.Add(new AdminAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = summary,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
