using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolMeetingProviderOptions;

public sealed record ListSchoolMeetingProviderOptionsQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SafeMeetingProviderOptionDto>>>;

public sealed class ListSchoolMeetingProviderOptionsQueryHandler(
    ISchoolPortalAccess portalAccess,
    INotificationRepository notificationRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolMeetingProviderOptionsQuery, Result<IReadOnlyList<SafeMeetingProviderOptionDto>>>
{
    public async Task<Result<IReadOnlyList<SafeMeetingProviderOptionDto>>> Handle(
        ListSchoolMeetingProviderOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<IReadOnlyList<SafeMeetingProviderOptionDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SafeMeetingProviderOptionDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var integrations = await notificationRepository.ListIntegrationsAsync(
            IntegrationType.Meeting,
            providerCode: null,
            isActive: true,
            healthStatus: null,
            cancellationToken);

        return Result<IReadOnlyList<SafeMeetingProviderOptionDto>>.Success(
            integrations
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ProviderCode, StringComparer.OrdinalIgnoreCase)
                .Select(item => new SafeMeetingProviderOptionDto(
                    item.ProviderCode,
                    item.DisplayNameAr,
                    item.DisplayNameEn))
                .ToArray());
    }
}
