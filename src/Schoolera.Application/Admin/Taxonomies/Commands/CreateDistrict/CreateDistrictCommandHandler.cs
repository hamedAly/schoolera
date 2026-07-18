using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateDistrict;

public sealed record CreateDistrictCommand(
    Guid CityId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<DistrictAdminDto>>;

public sealed class CreateDistrictCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateDistrictCommandHandler> logger)
    : IRequestHandler<CreateDistrictCommand, Result<DistrictAdminDto>>
{
    public async Task<Result<DistrictAdminDto>> Handle(
        CreateDistrictCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating district {Slug}.", request.Slug);

        if (!await taxonomyRepository.CityExistsAsync(request.CityId, cancellationToken))
        {
            return Result<DistrictAdminDto>.Failure(
                ["City not found."],
                [TaxonomyErrorCodes.CityNotFound]);
        }

        if (await taxonomyRepository.IsDistrictSlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<DistrictAdminDto>.Failure(
                ["District slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var district = new District(request.CityId, request.NameAr, request.NameEn, request.Slug, request.SortOrder);
        await taxonomyRepository.AddDistrictAsync(district, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DistrictAdminDto>.Success(DistrictAdminDto.FromDistrict(district));
    }
}
