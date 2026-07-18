using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateDistrict;

public sealed record UpdateDistrictCommand(
    Guid Id,
    Guid CityId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<DistrictAdminDto>>;

public sealed class UpdateDistrictCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateDistrictCommandHandler> logger)
    : IRequestHandler<UpdateDistrictCommand, Result<DistrictAdminDto>>
{
    public async Task<Result<DistrictAdminDto>> Handle(
        UpdateDistrictCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating district {DistrictId}.", request.Id);

        var district = await taxonomyRepository.GetDistrictByIdAsync(request.Id, cancellationToken);
        if (district is null)
        {
            return Result<DistrictAdminDto>.Failure(
                ["District not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (!await taxonomyRepository.CityExistsAsync(request.CityId, cancellationToken))
        {
            return Result<DistrictAdminDto>.Failure(
                ["City not found."],
                [TaxonomyErrorCodes.CityNotFound]);
        }

        if (await taxonomyRepository.IsDistrictSlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<DistrictAdminDto>.Failure(
                ["District slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        district.Update(
            request.CityId,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DistrictAdminDto>.Success(DistrictAdminDto.FromDistrict(district));
    }
}
