using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Notifications;

public sealed class NotificationRepository(SchooleraDbContext dbContext) : INotificationRepository
{
    public Task<PlatformIntegrationConfiguration?> GetIntegrationAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.PlatformIntegrationConfigurations
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PlatformIntegrationConfiguration>> ListIntegrationsAsync(
        IntegrationType? type,
        string? providerCode,
        bool? isActive,
        IntegrationHealthStatus? healthStatus,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PlatformIntegrationConfigurations.AsNoTracking().AsQueryable();

        if (type is not null)
        {
            query = query.Where(item => item.IntegrationType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            var code = providerCode.Trim();
            query = query.Where(item => item.ProviderCode == code);
        }

        if (isActive is not null)
        {
            query = query.Where(item => item.IsActive == isActive.Value);
        }

        if (healthStatus is not null)
        {
            query = query.Where(item => item.HealthStatus == healthStatus.Value);
        }

        return await query
            .OrderBy(item => item.IntegrationType)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayNameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task AddIntegrationAsync(
        PlatformIntegrationConfiguration entity,
        CancellationToken cancellationToken = default) =>
        await dbContext.PlatformIntegrationConfigurations.AddAsync(entity, cancellationToken);

    public async Task ClearDefaultAsync(
        IntegrationType type,
        Guid? exceptId,
        CancellationToken cancellationToken = default)
    {
        var defaults = await dbContext.PlatformIntegrationConfigurations
            .Where(item =>
                item.IntegrationType == type &&
                item.IsDefault &&
                (exceptId == null || item.Id != exceptId.Value))
            .ToListAsync(cancellationToken);

        foreach (var item in defaults)
        {
            item.ClearDefault();
        }
    }

    public async Task<ParentNotificationPreference> GetOrCreatePreferencesAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetPreferencesAsync(parentUserId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = new ParentNotificationPreference(parentUserId);
        await dbContext.ParentNotificationPreferences.AddAsync(created, cancellationToken);
        return created;
    }

    public Task<ParentNotificationPreference?> GetPreferencesAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.ParentNotificationPreferences
            .FirstOrDefaultAsync(item => item.ParentUserId == parentUserId, cancellationToken);

    public async Task AddOutboxAsync(
        NotificationOutboxMessage message,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationOutboxMessages.AddAsync(message, cancellationToken);

    public Task<bool> OutboxExistsAsync(
        string deduplicationKey,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationOutboxMessages
            .AnyAsync(item => item.DeduplicationKey == deduplicationKey, cancellationToken);

    public async Task<IReadOnlyList<NotificationOutboxMessage>> ClaimPendingBatchAsync(
        int batchSize,
        DateTimeOffset staleBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var candidates = await dbContext.NotificationOutboxMessages
                .Where(item =>
                    (item.Status == NotificationStatus.Pending && item.NextAttemptAtUtc <= now) ||
                    (item.Status == NotificationStatus.Failed && item.NextAttemptAtUtc <= now) ||
                    (item.Status == NotificationStatus.Processing &&
                     item.ProcessingStartedAtUtc != null &&
                     item.ProcessingStartedAtUtc < staleBeforeUtc))
                .OrderBy(item => item.NextAttemptAtUtc)
                .ThenBy(item => item.CreatedAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in candidates)
            {
                message.MarkProcessing();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (IReadOnlyList<NotificationOutboxMessage>)candidates;
        });
    }

    public async Task<IReadOnlyList<NotificationOutboxMessage>> ListParentInAppAsync(
        Guid parentUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);

        return await dbContext.NotificationOutboxMessages
            .AsNoTracking()
            .Where(item =>
                item.RecipientUserId == parentUserId &&
                item.Channel == NotificationChannel.InApp &&
                (item.Status == NotificationStatus.Sent ||
                 item.Status == NotificationStatus.Delivered))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountParentInAppAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationOutboxMessages
            .CountAsync(
                item =>
                    item.RecipientUserId == parentUserId &&
                    item.Channel == NotificationChannel.InApp &&
                    (item.Status == NotificationStatus.Sent ||
                     item.Status == NotificationStatus.Delivered),
                cancellationToken);

    public Task<int> CountParentUnreadAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationOutboxMessages
            .CountAsync(
                item =>
                    item.RecipientUserId == parentUserId &&
                    item.Channel == NotificationChannel.InApp &&
                    item.ReadAtUtc == null &&
                    (item.Status == NotificationStatus.Sent ||
                     item.Status == NotificationStatus.Delivered),
                cancellationToken);

    public Task<NotificationOutboxMessage?> GetParentInAppAsync(
        Guid parentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationOutboxMessages
            .FirstOrDefaultAsync(
                item =>
                    item.Id == notificationId &&
                    item.RecipientUserId == parentUserId &&
                    item.Channel == NotificationChannel.InApp,
                cancellationToken);

    public async Task MarkAllParentInAppReadAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default)
    {
        var unread = await dbContext.NotificationOutboxMessages
            .Where(item =>
                item.RecipientUserId == parentUserId &&
                item.Channel == NotificationChannel.InApp &&
                item.ReadAtUtc == null &&
                (item.Status == NotificationStatus.Sent ||
                 item.Status == NotificationStatus.Delivered))
            .ToListAsync(cancellationToken);

        foreach (var message in unread)
        {
            message.MarkRead();
        }
    }

    public async Task<IReadOnlyList<ParentAdmissionOpenSubscription>> ListSubscriptionsAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ParentAdmissionOpenSubscriptions
            .AsNoTracking()
            .Where(item => item.ParentUserId == parentUserId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ParentAdmissionOpenSubscription?> GetSubscriptionAsync(
        Guid parentUserId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default) =>
        dbContext.ParentAdmissionOpenSubscriptions
            .FirstOrDefaultAsync(
                item => item.Id == subscriptionId && item.ParentUserId == parentUserId,
                cancellationToken);

    public Task<ParentAdmissionOpenSubscription?> FindActiveSubscriptionAsync(
        Guid parentUserId,
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        CancellationToken cancellationToken = default) =>
        dbContext.ParentAdmissionOpenSubscriptions
            .FirstOrDefaultAsync(
                item =>
                    item.ParentUserId == parentUserId &&
                    item.SchoolId == schoolId &&
                    item.SchoolBranchId == branchId &&
                    item.EducationalStageId == stageId &&
                    item.GradeId == gradeId &&
                    item.AcademicYearId == academicYearId &&
                    item.IsActive,
                cancellationToken);

    public async Task AddSubscriptionAsync(
        ParentAdmissionOpenSubscription subscription,
        CancellationToken cancellationToken = default) =>
        await dbContext.ParentAdmissionOpenSubscriptions.AddAsync(subscription, cancellationToken);

    public async Task<IReadOnlyList<ParentAdmissionOpenSubscription>> FindMatchingActiveSubscriptionsAsync(
        Guid schoolId,
        Guid? branchId,
        Guid stageId,
        IReadOnlyList<Guid> gradeIds,
        CancellationToken cancellationToken = default)
    {
        var gradeSet = gradeIds.ToHashSet();

        var candidates = await dbContext.ParentAdmissionOpenSubscriptions
            .Where(item => item.SchoolId == schoolId && item.IsActive)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(item =>
                (item.SchoolBranchId is null || item.SchoolBranchId == branchId) &&
                (item.EducationalStageId is null || item.EducationalStageId == stageId) &&
                (item.GradeId is null || gradeSet.Contains(item.GradeId.Value)))
            .ToArray();
    }

    public Task<NotificationTemplate?> GetTemplateAsync(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        CancellationToken cancellationToken = default)
    {
        var normalized = culture.Trim().ToLowerInvariant();
        return dbContext.NotificationTemplates
            .Include(item => item.Versions)
            .FirstOrDefaultAsync(
                item =>
                    item.EventType == eventType &&
                    item.Channel == channel &&
                    item.Culture == normalized,
                cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationTemplates
            .AsNoTracking()
            .Include(item => item.Versions)
            .OrderBy(item => item.EventType)
            .ThenBy(item => item.Channel)
            .ThenBy(item => item.Culture)
            .ToListAsync(cancellationToken);

    public async Task AddTemplateAsync(
        NotificationTemplate template,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationTemplates.AddAsync(template, cancellationToken);

    public Task<NotificationTemplateVersion?> GetTemplateVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationTemplateVersions
            .Include(item => item.Template)
            .FirstOrDefaultAsync(item => item.Id == versionId, cancellationToken);

    public async Task<(int Pending, int Failed, int DeadLetter, int Processing)> GetOutboxCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.NotificationOutboxMessages
            .CountAsync(item => item.Status == NotificationStatus.Pending, cancellationToken);
        var failed = await dbContext.NotificationOutboxMessages
            .CountAsync(item => item.Status == NotificationStatus.Failed, cancellationToken);
        var deadLetter = await dbContext.NotificationOutboxMessages
            .CountAsync(item => item.Status == NotificationStatus.DeadLetter, cancellationToken);
        var processing = await dbContext.NotificationOutboxMessages
            .CountAsync(item => item.Status == NotificationStatus.Processing, cancellationToken);

        return (pending, failed, deadLetter, processing);
    }
}
