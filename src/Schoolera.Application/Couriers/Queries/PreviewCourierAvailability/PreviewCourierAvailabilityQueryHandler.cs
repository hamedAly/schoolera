using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Couriers.Queries.PreviewCourierAvailability;

public sealed record PreviewCourierAvailabilityQuery(
    Guid IntegrationId, Guid CountryId, Guid? GovernorateId, Guid? CityId, Guid? DistrictId)
    : IRequest<Result<IReadOnlyList<CourierAvailabilityOptionDto>>>;

public sealed class PreviewCourierAvailabilityQueryHandler(
    ICurrentUser currentUser,
    ICourierAdministrationService service)
    : IRequestHandler<PreviewCourierAvailabilityQuery, Result<IReadOnlyList<CourierAvailabilityOptionDto>>>
{
    public Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> Handle(
        PreviewCourierAvailabilityQuery r, CancellationToken ct) =>
        currentUser.UserId.HasValue && currentUser.IsInRole(SchooleraRoles.PlatformAdmin)
            ? service.PreviewAsync(r.IntegrationId, new(r.CountryId, r.GovernorateId, r.CityId, r.DistrictId), ct)
            : Task.FromResult(Result<IReadOnlyList<CourierAvailabilityOptionDto>>.Failure(
                ["Forbidden."], [CourierErrorCodes.Forbidden]));
}
