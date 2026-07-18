using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateCountry;

public sealed record CreateCountryCommand(
    string Code,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<CountryAdminDto>>;

public sealed class CreateCountryCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCountryCommandHandler> logger)
    : IRequestHandler<CreateCountryCommand, Result<CountryAdminDto>>
{
    public async Task<Result<CountryAdminDto>> Handle(
        CreateCountryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating country {Slug}.", request.Slug);

        if (await taxonomyRepository.IsCountryCodeTakenAsync(request.Code, cancellationToken: cancellationToken))
        {
            return Result<CountryAdminDto>.Failure(
                ["Country code is already taken."],
                [TaxonomyErrorCodes.CodeDuplicate]);
        }

        if (await taxonomyRepository.IsCountrySlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<CountryAdminDto>.Failure(
                ["Country slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var country = new Country(request.Code, request.NameAr, request.NameEn, request.Slug, request.SortOrder);
        await taxonomyRepository.AddCountryAsync(country, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CountryAdminDto>.Success(CountryAdminDto.FromCountry(country));
    }
}
