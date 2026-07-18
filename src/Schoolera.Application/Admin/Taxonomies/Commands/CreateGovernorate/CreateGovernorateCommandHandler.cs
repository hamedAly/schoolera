using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateGovernorate;

public sealed record CreateGovernorateCommand(
    Guid CountryId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<GovernorateAdminDto>>;

public sealed class CreateGovernorateCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateGovernorateCommandHandler> logger)
    : IRequestHandler<CreateGovernorateCommand, Result<GovernorateAdminDto>>
{
    public async Task<Result<GovernorateAdminDto>> Handle(
        CreateGovernorateCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating governorate {Slug}.", request.Slug);

        if (!await taxonomyRepository.CountryExistsAsync(request.CountryId, cancellationToken))
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Country not found."],
                [TaxonomyErrorCodes.CountryNotFound]);
        }

        if (await taxonomyRepository.IsGovernorateSlugTakenAsync(
                request.CountryId,
                request.Slug,
                cancellationToken: cancellationToken))
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Governorate slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var governorate = new Governorate(
            request.CountryId,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder);
        await taxonomyRepository.AddGovernorateAsync(governorate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GovernorateAdminDto>.Success(GovernorateAdminDto.FromGovernorate(governorate));
    }
}
