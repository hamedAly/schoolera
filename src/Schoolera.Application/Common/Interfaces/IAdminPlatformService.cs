using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface IAdminPlatformService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AdminSchoolListItemDto>> ListSchoolsAsync(
        string? search,
        SchoolStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<AdminSchoolDetailDto?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<Result<AdminSchoolDetailDto>> UpdateSchoolStatusAsync(
        Guid schoolId,
        SchoolStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(
        string? search,
        string? role,
        AccountStatus? accountStatus,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserListItemDto>> UpdateUserAccountStatusAsync(
        Guid userId,
        AccountStatus accountStatus,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminAuditEventDto>> ListAuditEventsAsync(
        string? action,
        string? entityType,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task WriteAuditAsync(
        Guid actorUserId,
        string action,
        string entityType,
        string? entityId,
        string? summary,
        CancellationToken cancellationToken = default);
}
