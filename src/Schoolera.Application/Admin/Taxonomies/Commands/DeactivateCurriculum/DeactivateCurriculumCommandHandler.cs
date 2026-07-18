using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCurriculum;

public sealed record DeactivateCurriculumCommand(Guid Id) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class DeactivateCurriculumCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateCurriculumCommandHandler> logger)
    : IRequestHandler<DeactivateCurriculumCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        DeactivateCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating curriculum {CurriculumId}.", request.Id);

        var curriculum = await taxonomyRepository.GetCurriculumByIdAsync(request.Id, cancellationToken);
        if (curriculum is null)
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Curriculum not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        curriculum.Update(
            curriculum.NameAr,
            curriculum.NameEn,
            curriculum.Slug,
            curriculum.DescriptionAr,
            curriculum.DescriptionEn,
            curriculum.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromCurriculum(curriculum));
    }
}
