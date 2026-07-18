using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admin;

public sealed class AdminPlatformService(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<AuthMessages> localizer,
    IAdmissionApplicationRepository admissionRepository) : IAdminPlatformService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var schoolCounts = await dbContext.Schools
            .AsNoTracking()
            .GroupBy(school => school.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int CountStatus(SchoolStatus status) =>
            schoolCounts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        var totalSchools = schoolCounts.Sum(item => item.Count);

        var onboardingCounts = await dbContext.SchoolOnboardingApplications
            .AsNoTracking()
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int CountOnboarding(SchoolOnboardingStatus status) =>
            onboardingCounts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        var totalUsers = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken);

        var activeParents = await CountActiveUsersInRoleAsync(SchooleraRoles.Parent, cancellationToken);
        var activeOwners = await CountActiveUsersInRoleAsync(SchooleraRoles.SchoolOwner, cancellationToken);
        var activeAdmins = await CountActiveUsersInRoleAsync(SchooleraRoles.SchoolAdmin, cancellationToken);

        var recentAudit = await ListRecentAuditAsync(8, cancellationToken);
        var admissionMetrics = await admissionRepository.GetAdminDashboardMetricsAsync(cancellationToken);

        return new AdminDashboardDto(
            TotalSchools: totalSchools,
            PublishedSchools: CountStatus(SchoolStatus.Published),
            UnpublishedSchools: CountStatus(SchoolStatus.Unpublished),
            SuspendedSchools: CountStatus(SchoolStatus.Suspended),
            DraftSchools: CountStatus(SchoolStatus.Draft),
            PendingOnboardingApplications: CountOnboarding(SchoolOnboardingStatus.Submitted),
            UnderReviewOnboardingApplications: CountOnboarding(SchoolOnboardingStatus.UnderReview),
            ChangesRequestedApplications: CountOnboarding(SchoolOnboardingStatus.ChangesRequested),
            ActiveParents: activeParents,
            ActiveSchoolOwners: activeOwners,
            ActiveSchoolAdmins: activeAdmins,
            TotalUsers: totalUsers,
            AdmissionsAvailable: true,
            AdmissionApplicationsCount: admissionMetrics.TotalApplications,
            AdmissionDraftCount: admissionMetrics.Draft,
            AdmissionSubmittedCount: admissionMetrics.Submitted,
            AdmissionUnderReviewCount: admissionMetrics.UnderReview,
            AdmissionMissingDocumentsCount: admissionMetrics.MissingDocuments,
            AdmissionInterviewRequiredCount: admissionMetrics.InterviewRequired,
            AdmissionAssessmentRequiredCount: admissionMetrics.AssessmentRequired,
            AdmissionWaitingListCount: admissionMetrics.WaitingList,
            AdmissionAcceptedCount: admissionMetrics.Accepted,
            AdmissionRejectedCount: admissionMetrics.Rejected,
            AdmissionCancelledCount: admissionMetrics.Cancelled,
            AdmissionRegisteredCount: admissionMetrics.Registered,
            AdmissionApplicationsTodayCount: admissionMetrics.ApplicationsToday,
            AdmissionPendingSchoolReviewCount: admissionMetrics.PendingSchoolReview,
            RecentAuditEvents: recentAudit);
    }

    public async Task<PagedResult<AdminSchoolListItemDto>> ListSchoolsAsync(
        string? search,
        SchoolStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Schools.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(school => school.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(school =>
                school.NameAr.Contains(term) ||
                (school.NameEn != null && school.NameEn.Contains(term)) ||
                school.Slug.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(school => school.UpdatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(school => new AdminSchoolListItemDto(
                school.Id,
                school.NameAr,
                school.NameEn ?? string.Empty,
                school.Slug,
                school.Status.ToString(),
                school.SchoolType.ToString(),
                school.CreatedAtUtc,
                school.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return PagedResult<AdminSchoolListItemDto>.Create(items, totalCount, paging);
    }

    public async Task<AdminSchoolDetailDto?> GetSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var school = await dbContext.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == schoolId, cancellationToken);

        if (school is null)
        {
            return null;
        }

        string? ownerEmail = null;
        if (school.OwnerUserId is { } ownerId)
        {
            ownerEmail = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == ownerId)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapSchoolDetail(school, ownerEmail);
    }

    public async Task<Result<AdminSchoolDetailDto>> UpdateSchoolStatusAsync(
        Guid schoolId,
        SchoolStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var school = await dbContext.Schools
            .FirstOrDefaultAsync(item => item.Id == schoolId, cancellationToken);

        if (school is null)
        {
            return Result<AdminSchoolDetailDto>.Failure(
                [localizer["AdminSchoolNotFound"].Value],
                [AdminErrorCodes.SchoolNotFound]);
        }

        var previous = school.Status;
        if (previous != status)
        {
            school.SetStatus(status);
            dbContext.AdminAuditEvents.Add(new AdminAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = AdminAuditActions.SchoolStatusChanged,
                EntityType = "School",
                EntityId = school.Id.ToString(),
                Summary = $"{previous} → {status}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        string? ownerEmail = null;
        if (school.OwnerUserId is { } ownerId)
        {
            ownerEmail = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == ownerId)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Result<AdminSchoolDetailDto>.Success(MapSchoolDetail(school, ownerEmail));
    }

    public async Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(
        string? search,
        string? role,
        AccountStatus? accountStatus,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = dbContext.Users.AsNoTracking();

        if (accountStatus is not null)
        {
            query = query.Where(user => user.AccountStatus == accountStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                (user.Email != null && user.Email.Contains(term)) ||
                user.FirstName.Contains(term) ||
                user.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            var roleId = await dbContext.Roles
                .AsNoTracking()
                .Where(item => item.Name == roleName)
                .Select(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleId == Guid.Empty)
            {
                return PagedResult<AdminUserListItemDto>.Create([], 0, paging);
            }

            query = from user in query
                    join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
                    where userRole.RoleId == roleId
                    select user;
            query = query.Distinct();
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageUsers = await query
            .OrderByDescending(user => user.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserListItemDto>(pageUsers.Count);
        foreach (var user in pageUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(MapUser(user, roles));
        }

        return PagedResult<AdminUserListItemDto>.Create(items, totalCount, paging);
    }

    public async Task<Result<AdminUserListItemDto>> UpdateUserAccountStatusAsync(
        Guid userId,
        AccountStatus accountStatus,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            return Result<AdminUserListItemDto>.Failure(
                [localizer["AdminCannotModifySelf"].Value],
                [AdminErrorCodes.CannotModifySelf]);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<AdminUserListItemDto>.Failure(
                [localizer["AdminUserNotFound"].Value],
                [AdminErrorCodes.UserNotFound]);
        }

        var previous = user.AccountStatus;
        if (previous != accountStatus)
        {
            user.AccountStatus = accountStatus;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result<AdminUserListItemDto>.Failure(
                    updateResult.Errors.Select(error => error.Description).ToArray(),
                    [AdminErrorCodes.InvalidAccountStatus]);
            }

            dbContext.AdminAuditEvents.Add(new AdminAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = AdminAuditActions.UserStatusChanged,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Summary = $"{previous} → {accountStatus}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var roles = await userManager.GetRolesAsync(user);
        return Result<AdminUserListItemDto>.Success(MapUser(user, roles));
    }

    public async Task<PagedResult<AdminAuditEventDto>> ListAuditEventsAsync(
        string? action,
        string? entityType,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AdminAuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var term = action.Trim();
            query = query.Where(item => item.Action == term);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var term = entityType.Trim();
            query = query.Where(item => item.EntityType == term);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await (
            from audit in query
            join user in dbContext.Users.AsNoTracking() on audit.ActorUserId equals user.Id into actors
            from actor in actors.DefaultIfEmpty()
            orderby audit.CreatedAtUtc descending
            select new
            {
                audit.Id,
                audit.ActorUserId,
                ActorEmail = actor != null ? actor.Email : null,
                audit.Action,
                audit.EntityType,
                audit.EntityId,
                audit.Summary,
                audit.CreatedAtUtc,
            })
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdminAuditEventDto(
                row.Id,
                row.ActorUserId,
                row.ActorEmail,
                row.Action,
                row.EntityType,
                row.EntityId,
                row.Summary,
                row.CreatedAtUtc))
            .ToList();

        return PagedResult<AdminAuditEventDto>.Create(items, totalCount, paging);
    }

    public async Task WriteAuditAsync(
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

    private async Task<int> CountActiveUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var roleId = await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId == Guid.Empty)
        {
            return 0;
        }

        return await (
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            where userRole.RoleId == roleId && user.AccountStatus == AccountStatus.Active
            select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdminAuditEventDto>> ListRecentAuditAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from audit in dbContext.AdminAuditEvents.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on audit.ActorUserId equals user.Id into actors
            from actor in actors.DefaultIfEmpty()
            orderby audit.CreatedAtUtc descending
            select new AdminAuditEventDto(
                audit.Id,
                audit.ActorUserId,
                actor != null ? actor.Email : null,
                audit.Action,
                audit.EntityType,
                audit.EntityId,
                audit.Summary,
                audit.CreatedAtUtc))
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static AdminSchoolDetailDto MapSchoolDetail(School school, string? ownerEmail) =>
        new(
            school.Id,
            school.NameAr,
            school.NameEn ?? string.Empty,
            school.Slug,
            school.Status.ToString(),
            school.SchoolType.ToString(),
            school.OwnerUserId,
            ownerEmail,
            school.ShortDescriptionAr,
            school.ShortDescriptionEn,
            school.CreatedAtUtc,
            school.UpdatedAtUtc);

    private static AdminUserListItemDto MapUser(ApplicationUser user, IList<string> roles) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AccountStatus.ToString(),
            roles.ToList(),
            user.PreferredLanguage,
            user.CreatedAtUtc,
            user.LastLoginAtUtc);
}
