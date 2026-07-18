using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateFacility;

public sealed record DeactivateFacilityCommand(Guid Id) : IRequest<Result<FacilityAdminDto>>;

public sealed class DeactivateFacilityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateFacilityCommandHandler> logger)
    : IRequestHandler<DeactivateFacilityCommand, Result<FacilityAdminDto>>
{
    public async Task<Result<FacilityAdminDto>> Handle(
        DeactivateFacilityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating facility {FacilityId}.", request.Id);

        var facility = await taxonomyRepository.GetFacilityByIdAsync(request.Id, cancellationToken);
        if (facility is null)
        {
            return Result<FacilityAdminDto>.Failure(
                ["Facility not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        facility.Update(
            facility.NameAr,
            facility.NameEn,
            facility.Slug,
            facility.IconKey,
            facility.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<FacilityAdminDto>.Success(FacilityAdminDto.FromFacility(facility));
    }
}
