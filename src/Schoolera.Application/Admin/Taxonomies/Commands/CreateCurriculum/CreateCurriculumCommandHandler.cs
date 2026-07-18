using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateCurriculum;

public sealed record CreateCurriculumCommand(
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<TaxonomyAdminDto>>;

public sealed class CreateCurriculumCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCurriculumCommandHandler> logger)
    : IRequestHandler<CreateCurriculumCommand, Result<TaxonomyAdminDto>>
{
    public async Task<Result<TaxonomyAdminDto>> Handle(
        CreateCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating curriculum {Slug}.", request.Slug);

        if (await taxonomyRepository.IsCurriculumSlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<TaxonomyAdminDto>.Failure(
                ["Curriculum slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var curriculum = new Curriculum(request.NameAr, request.NameEn, request.Slug, request.SortOrder);
        await taxonomyRepository.AddCurriculumAsync(curriculum, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaxonomyAdminDto>.Success(TaxonomyAdminDto.FromCurriculum(curriculum));
    }
}
