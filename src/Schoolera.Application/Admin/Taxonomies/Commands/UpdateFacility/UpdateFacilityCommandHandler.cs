using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateFacility;

public sealed record UpdateFacilityCommand(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    string? IconKey,
    int SortOrder,
    bool IsActive) : IRequest<Result<FacilityAdminDto>>;

public sealed class UpdateFacilityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateFacilityCommandHandler> logger)
    : IRequestHandler<UpdateFacilityCommand, Result<FacilityAdminDto>>
{
    public async Task<Result<FacilityAdminDto>> Handle(
        UpdateFacilityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating facility {FacilityId}.", request.Id);

        var facility = await taxonomyRepository.GetFacilityByIdAsync(request.Id, cancellationToken);
        if (facility is null)
        {
            return Result<FacilityAdminDto>.Failure(
                ["Facility not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (await taxonomyRepository.IsFacilitySlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<FacilityAdminDto>.Failure(
                ["Facility slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        facility.Update(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.IconKey,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<FacilityAdminDto>.Success(FacilityAdminDto.FromFacility(facility));
    }
}
