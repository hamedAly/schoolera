using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateCountry;

public sealed record UpdateCountryCommand(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<CountryAdminDto>>;

public sealed class UpdateCountryCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCountryCommandHandler> logger)
    : IRequestHandler<UpdateCountryCommand, Result<CountryAdminDto>>
{
    public async Task<Result<CountryAdminDto>> Handle(
        UpdateCountryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating country {CountryId}.", request.Id);

        var country = await taxonomyRepository.GetCountryByIdAsync(request.Id, cancellationToken);
        if (country is null)
        {
            return Result<CountryAdminDto>.Failure(
                ["Country not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (await taxonomyRepository.IsCountryCodeTakenAsync(
                request.Code,
                request.Id,
                cancellationToken))
        {
            return Result<CountryAdminDto>.Failure(
                ["Country code is already taken."],
                [TaxonomyErrorCodes.CodeDuplicate]);
        }

        if (await taxonomyRepository.IsCountrySlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<CountryAdminDto>.Failure(
                ["Country slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        country.Update(
            request.Code,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CountryAdminDto>.Success(CountryAdminDto.FromCountry(country));
    }
}
