using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateCity;

public sealed record CreateCityCommand(
    Guid GovernorateId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<CityAdminDto>>;

public sealed class CreateCityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCityCommandHandler> logger)
    : IRequestHandler<CreateCityCommand, Result<CityAdminDto>>
{
    public async Task<Result<CityAdminDto>> Handle(
        CreateCityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating city {Slug}.", request.Slug);

        if (!await taxonomyRepository.GovernorateExistsAsync(request.GovernorateId, cancellationToken))
        {
            return Result<CityAdminDto>.Failure(
                ["Governorate not found."],
                [TaxonomyErrorCodes.GovernorateNotFound]);
        }

        if (await taxonomyRepository.IsCitySlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<CityAdminDto>.Failure(
                ["City slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var city = new City(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.GovernorateId);
        await taxonomyRepository.AddCityAsync(city, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CityAdminDto>.Success(CityAdminDto.FromCity(city));
    }
}
