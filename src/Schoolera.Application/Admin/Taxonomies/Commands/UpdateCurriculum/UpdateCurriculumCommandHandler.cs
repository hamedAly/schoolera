using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateCurriculum;

public sealed record UpdateCurriculumCommand(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class UpdateCurriculumCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCurriculumCommandHandler> logger)
    : IRequestHandler<UpdateCurriculumCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        UpdateCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating curriculum {CurriculumId}.", request.Id);

        var curriculum = await taxonomyRepository.GetCurriculumByIdAsync(request.Id, cancellationToken);
        if (curriculum is null)
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Curriculum not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (await taxonomyRepository.IsCurriculumSlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Curriculum slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        curriculum.Update(
            request.NameAr,
            request.NameEn,
            request.Slug,
            curriculum.DescriptionAr,
            curriculum.DescriptionEn,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromCurriculum(curriculum));
    }
}
