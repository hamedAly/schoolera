using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCountry;

public sealed record DeactivateCountryCommand(Guid Id) : IRequest<Result<CountryAdminDto>>;

public sealed class DeactivateCountryCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateCountryCommandHandler> logger)
    : IRequestHandler<DeactivateCountryCommand, Result<CountryAdminDto>>
{
    public async Task<Result<CountryAdminDto>> Handle(
        DeactivateCountryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating country {CountryId}.", request.Id);

        var country = await taxonomyRepository.GetCountryByIdAsync(request.Id, cancellationToken);
        if (country is null)
        {
            return Result<CountryAdminDto>.Failure(
                ["Country not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        country.Update(
            country.Code,
            country.NameAr,
            country.NameEn,
            country.Slug,
            country.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CountryAdminDto>.Success(CountryAdminDto.FromCountry(country));
    }
}
