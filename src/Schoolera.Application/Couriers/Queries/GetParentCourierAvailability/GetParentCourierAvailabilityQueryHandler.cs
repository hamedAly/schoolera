using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Couriers.Queries.GetParentCourierAvailability;

public sealed record GetParentCourierAvailabilityQuery(
    Guid ApplicationId, Guid CountryId, Guid? GovernorateId, Guid? CityId, Guid? DistrictId)
    : IRequest<Result<ParentCourierAvailabilityDto>>;

public sealed class GetParentCourierAvailabilityQueryHandler(
    ICurrentUser currentUser,
    ICourierAdmissionContextReader contextReader,
    ICourierAvailabilityResolver resolver)
    : IRequestHandler<GetParentCourierAvailabilityQuery, Result<ParentCourierAvailabilityDto>>
{
    public async Task<Result<ParentCourierAvailabilityDto>> Handle(
        GetParentCourierAvailabilityQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
            return Result<ParentCourierAvailabilityDto>.Failure(["Forbidden."], [CourierErrorCodes.Forbidden]);
        var destination = await contextReader.GetOwnedDestinationAsync(userId, request.ApplicationId, ct);
        if (destination is null)
            return Result<ParentCourierAvailabilityDto>.Failure(["Application not found."], [CourierErrorCodes.NotFound]);
        if (destination.Status == AdmissionApplicationStatus.Cancelled)
            return Result<ParentCourierAvailabilityDto>.Failure(["Application is cancelled."], [CourierErrorCodes.ApplicationCancelled]);
        if (!destination.BranchIsActive)
            return Result<ParentCourierAvailabilityDto>.Failure(["Destination branch is inactive."], [CourierErrorCodes.BranchInactive]);

        var resolved = await resolver.ResolveAsync(
            new(new(request.CountryId, request.GovernorateId, request.CityId, request.DistrictId), DateTimeOffset.UtcNow), ct);
        if (!resolved.Succeeded)
            return Result<ParentCourierAvailabilityDto>.Failure(resolved.Errors, resolved.ErrorCodes);
        var branch = new CourierDestinationBranchDto(
            destination.BranchNameAr, destination.BranchNameEn, destination.AddressAr, destination.AddressEn,
            destination.CityNameAr, destination.CityNameEn, destination.DistrictNameAr, destination.DistrictNameEn);
        var reasons = resolved.Data is { Count: > 0 } ? [] : new[] { "courier.noAvailableOptions" };
        return Result<ParentCourierAvailabilityDto>.Success(new(branch, resolved.Data ?? [], reasons));
    }
}
