using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateEducationalStage;

public sealed record DeactivateEducationalStageCommand(Guid Id) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class DeactivateEducationalStageCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateEducationalStageCommandHandler> logger)
    : IRequestHandler<DeactivateEducationalStageCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        DeactivateEducationalStageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating educational stage {StageId}.", request.Id);

        var stage = await taxonomyRepository.GetEducationalStageByIdAsync(request.Id, cancellationToken);
        if (stage is null)
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Educational stage not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        stage.Update(stage.NameAr, stage.NameEn, stage.Slug, stage.SortOrder, isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromEducationalStage(stage));
    }
}
