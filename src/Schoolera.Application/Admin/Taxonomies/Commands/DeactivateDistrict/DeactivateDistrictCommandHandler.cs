using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateDistrict;

public sealed record DeactivateDistrictCommand(Guid Id) : IRequest<Result<DistrictAdminDto>>;

public sealed class DeactivateDistrictCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateDistrictCommandHandler> logger)
    : IRequestHandler<DeactivateDistrictCommand, Result<DistrictAdminDto>>
{
    public async Task<Result<DistrictAdminDto>> Handle(
        DeactivateDistrictCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating district {DistrictId}.", request.Id);

        var district = await taxonomyRepository.GetDistrictByIdAsync(request.Id, cancellationToken);
        if (district is null)
        {
            return Result<DistrictAdminDto>.Failure(
                ["District not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        district.Update(
            district.CityId,
            district.NameAr,
            district.NameEn,
            district.Slug,
            district.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DistrictAdminDto>.Success(DistrictAdminDto.FromDistrict(district));
    }
}
