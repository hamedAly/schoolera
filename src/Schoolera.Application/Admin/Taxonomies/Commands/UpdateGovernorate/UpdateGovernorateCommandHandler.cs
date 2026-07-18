using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateGovernorate;

public sealed record UpdateGovernorateCommand(
    Guid Id,
    Guid CountryId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<GovernorateAdminDto>>;

public sealed class UpdateGovernorateCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateGovernorateCommandHandler> logger)
    : IRequestHandler<UpdateGovernorateCommand, Result<GovernorateAdminDto>>
{
    public async Task<Result<GovernorateAdminDto>> Handle(
        UpdateGovernorateCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating governorate {GovernorateId}.", request.Id);

        var governorate = await taxonomyRepository.GetGovernorateByIdAsync(request.Id, cancellationToken);
        if (governorate is null)
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Governorate not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (!await taxonomyRepository.CountryExistsAsync(request.CountryId, cancellationToken))
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Country not found."],
                [TaxonomyErrorCodes.CountryNotFound]);
        }

        if (await taxonomyRepository.IsGovernorateSlugTakenAsync(
                request.CountryId,
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Governorate slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        governorate.Update(
            request.CountryId,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GovernorateAdminDto>.Success(GovernorateAdminDto.FromGovernorate(governorate));
    }
}
