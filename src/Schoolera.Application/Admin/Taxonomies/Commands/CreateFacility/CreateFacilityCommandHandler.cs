using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateFacility;

public sealed record CreateFacilityCommand(
    string NameAr,
    string? NameEn,
    string Slug,
    string? IconKey,
    int SortOrder) : IRequest<Result<FacilityAdminDto>>;

public sealed class CreateFacilityCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateFacilityCommandHandler> logger)
    : IRequestHandler<CreateFacilityCommand, Result<FacilityAdminDto>>
{
    public async Task<Result<FacilityAdminDto>> Handle(
        CreateFacilityCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating facility {Slug}.", request.Slug);

        if (await taxonomyRepository.IsFacilitySlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<FacilityAdminDto>.Failure(
                ["Facility slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var facility = new Facility(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IconKey);
        await taxonomyRepository.AddFacilityAsync(facility, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<FacilityAdminDto>.Success(FacilityAdminDto.FromFacility(facility));
    }
}
