using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Couriers.Commands.MutateCourier;

public enum CourierMutationKind
{
    UpdateProfile, CreateService, UpdateService, SetServiceActive,
    CreateCoverage, UpdateCoverage, SetCoverageActive,
    CreateWindow, UpdateWindow, SetWindowActive,
    CreateSla, UpdateSla, SetSlaActive,
}

public sealed record MutateCourierCommand(
    Guid IntegrationId,
    CourierMutationKind Kind,
    Guid? ChildId = null,
    UpdateCourierProfileRequest? Profile = null,
    UpsertCourierServiceRequest? Service = null,
    UpsertCourierCoverageRequest? Coverage = null,
    UpsertCourierWindowRequest? Window = null,
    UpsertCourierSlaRequest? Sla = null,
    SetCourierChildActiveRequest? Active = null) : IRequest<Result<CourierAdminDetailDto>>;

public sealed class MutateCourierCommandHandler(
    ICurrentUser currentUser,
    ICourierAdministrationService service,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<MutateCourierCommandHandler> logger)
    : IRequestHandler<MutateCourierCommand, Result<CourierAdminDetailDto>>
{
    public async Task<Result<CourierAdminDetailDto>> Handle(MutateCourierCommand r, CancellationToken ct)
    {
        _ = unitOfWork;
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
            return Result<CourierAdminDetailDto>.Failure(["Forbidden."], [CourierErrorCodes.Forbidden]);

        var result = r.Kind switch
        {
            CourierMutationKind.UpdateProfile => await service.UpdateProfileAsync(r.IntegrationId, r.Profile!, ct),
            CourierMutationKind.CreateService => await service.CreateServiceAsync(r.IntegrationId, r.Service!, ct),
            CourierMutationKind.UpdateService => await service.UpdateServiceAsync(r.IntegrationId, r.ChildId!.Value, r.Service!, ct),
            CourierMutationKind.SetServiceActive => await service.SetServiceActiveAsync(r.IntegrationId, r.ChildId!.Value, r.Active!, ct),
            CourierMutationKind.CreateCoverage => await service.CreateCoverageAsync(r.IntegrationId, r.Coverage!, ct),
            CourierMutationKind.UpdateCoverage => await service.UpdateCoverageAsync(r.IntegrationId, r.ChildId!.Value, r.Coverage!, ct),
            CourierMutationKind.SetCoverageActive => await service.SetCoverageActiveAsync(r.IntegrationId, r.ChildId!.Value, r.Active!, ct),
            CourierMutationKind.CreateWindow => await service.CreateWindowAsync(r.IntegrationId, r.Window!, ct),
            CourierMutationKind.UpdateWindow => await service.UpdateWindowAsync(r.IntegrationId, r.ChildId!.Value, r.Window!, ct),
            CourierMutationKind.SetWindowActive => await service.SetWindowActiveAsync(r.IntegrationId, r.ChildId!.Value, r.Active!, ct),
            CourierMutationKind.CreateSla => await service.CreateSlaAsync(r.IntegrationId, r.Sla!, ct),
            CourierMutationKind.UpdateSla => await service.UpdateSlaAsync(r.IntegrationId, r.ChildId!.Value, r.Sla!, ct),
            CourierMutationKind.SetSlaActive => await service.SetSlaActiveAsync(r.IntegrationId, r.ChildId!.Value, r.Active!, ct),
            _ => Result<CourierAdminDetailDto>.Failure(["Unsupported courier operation."], ["courier.invalidOperation"]),
        };

        if (result.Succeeded)
        {
            var action = $"courier.{r.Kind.ToString().ToLowerInvariant()}";
            await adminPlatform.WriteAuditAsync(
                actorId, action, "CourierConfiguration",
                r.ChildId?.ToString() ?? r.IntegrationId.ToString(),
                $"integrationId={r.IntegrationId}; childId={r.ChildId}", ct);
            logger.LogInformation(
                "Courier admin operation {Operation} completed for integration {IntegrationId} child {ChildId}.",
                r.Kind, r.IntegrationId, r.ChildId);
        }
        return result;
    }
}
