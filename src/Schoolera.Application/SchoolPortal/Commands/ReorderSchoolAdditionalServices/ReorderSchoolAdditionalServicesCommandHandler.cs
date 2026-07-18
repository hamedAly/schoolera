using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdditionalServices;

public sealed record ReorderSchoolAdditionalServicesCommand(
    Guid SchoolId,
    ReorderSchoolAdditionalServicesRequest Body) : IRequest<Result<IReadOnlyList<SchoolAdditionalServiceDto>>>;

public sealed class ReorderSchoolAdditionalServicesCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReorderSchoolAdditionalServicesCommandHandler> logger)
    : IRequestHandler<ReorderSchoolAdditionalServicesCommand, Result<IReadOnlyList<SchoolAdditionalServiceDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdditionalServiceDto>>> Handle(
        ReorderSchoolAdditionalServicesCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolAdditionalServiceDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolAdditionalServiceDto>>(
            accessResult.Data, SchoolPortalPermission.ManageServices, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var services = await repository.ListServicesAsync(request.SchoolId, cancellationToken);
        var serviceIds = request.Body.ServiceIds;
        if (serviceIds.Count != services.Count ||
            services.Select(service => service.Id).ToHashSet().SetEquals(serviceIds) == false)
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdditionalServiceDto>>(
                localizer, SchoolPortalErrorCodes.InvalidReorder);
        }

        var servicesById = services.ToDictionary(service => service.Id);
        for (var index = 0; index < serviceIds.Count; index++)
        {
            var tracked = await repository.GetServiceForWriteAsync(
                request.SchoolId, serviceIds[index], cancellationToken);
            if (tracked is null)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdditionalServiceDto>>(
                    localizer, SchoolPortalErrorCodes.ServiceNotFound);
            }

            tracked.SetSortOrder(index);
            servicesById[tracked.Id] = tracked;
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolAdditionalServiceDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var ordered = serviceIds
            .Select(id => SchoolPortalReadModel.ToService(servicesById[id]))
            .ToArray();

        return Result<IReadOnlyList<SchoolAdditionalServiceDto>>.Success(ordered);
    }
}
