using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCity;

public sealed record DeactivateCityCommand(Guid Id) : IRequest<Result<CityAdminDto>>;

public sealed class DeactivateCityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateCityCommandHandler> logger)
    : IRequestHandler<DeactivateCityCommand, Result<CityAdminDto>>
{
    public async Task<Result<CityAdminDto>> Handle(
        DeactivateCityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating city {CityId}.", request.Id);

        var city = await taxonomyRepository.GetCityByIdAsync(request.Id, cancellationToken);
        if (city is null)
        {
            return Result<CityAdminDto>.Failure(
                ["City not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        city.Update(
            city.NameAr,
            city.NameEn,
            city.Slug,
            city.SortOrder,
            isActive: false,
            city.GovernorateId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CityAdminDto>.Success(CityAdminDto.FromCity(city));
    }
}
