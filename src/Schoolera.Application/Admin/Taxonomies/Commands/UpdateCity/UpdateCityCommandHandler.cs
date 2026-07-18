using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateCity;

public sealed record UpdateCityCommand(
    Guid Id,
    Guid? GovernorateId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<CityAdminDto>>;

public sealed class UpdateCityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCityCommandHandler> logger)
    : IRequestHandler<UpdateCityCommand, Result<CityAdminDto>>
{
    public async Task<Result<CityAdminDto>> Handle(
        UpdateCityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating city {CityId}.", request.Id);

        var city = await taxonomyRepository.GetCityByIdAsync(request.Id, cancellationToken);
        if (city is null)
        {
            return Result<CityAdminDto>.Failure(
                ["City not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (request.GovernorateId is { } governorateId
            && !await taxonomyRepository.GovernorateExistsAsync(governorateId, cancellationToken))
        {
            return Result<CityAdminDto>.Failure(
                ["Governorate not found."],
                [TaxonomyErrorCodes.GovernorateNotFound]);
        }

        if (await taxonomyRepository.IsCitySlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<CityAdminDto>.Failure(
                ["City slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        city.Update(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IsActive,
            request.GovernorateId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CityAdminDto>.Success(CityAdminDto.FromCity(city));
    }
}
