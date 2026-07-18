using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateEducationalStage;

public sealed record CreateEducationalStageCommand(
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class CreateEducationalStageCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateEducationalStageCommandHandler> logger)
    : IRequestHandler<CreateEducationalStageCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        CreateEducationalStageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating educational stage {Slug}.", request.Slug);

        if (await taxonomyRepository.IsEducationalStageSlugTakenAsync(
                request.Slug,
                cancellationToken: cancellationToken))
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Educational stage slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var stage = new EducationalStage(request.NameAr, request.NameEn, request.Slug, request.SortOrder);
        await taxonomyRepository.AddEducationalStageAsync(stage, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromEducationalStage(stage));
    }
}
