using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateEducationalStage;

public sealed record UpdateEducationalStageCommand(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class UpdateEducationalStageCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateEducationalStageCommandHandler> logger)
    : IRequestHandler<UpdateEducationalStageCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        UpdateEducationalStageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating educational stage {StageId}.", request.Id);

        var stage = await taxonomyRepository.GetEducationalStageByIdAsync(request.Id, cancellationToken);
        if (stage is null)
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Educational stage not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (await taxonomyRepository.IsEducationalStageSlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Educational stage slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        stage.Update(request.NameAr, request.NameEn, request.Slug, request.SortOrder, request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromEducationalStage(stage));
    }
}
