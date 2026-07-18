using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateGrade;

public sealed record CreateGradeCommand(
    Guid EducationalStageId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder) : IRequest<Result<GradeAdminDto>>;

public sealed class CreateGradeCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateGradeCommandHandler> logger)
    : IRequestHandler<CreateGradeCommand, Result<GradeAdminDto>>
{
    public async Task<Result<GradeAdminDto>> Handle(
        CreateGradeCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating grade {Slug}.", request.Slug);

        if (!await taxonomyRepository.EducationalStageExistsAsync(
                request.EducationalStageId,
                cancellationToken))
        {
            return Result<GradeAdminDto>.Failure(
                ["Educational stage not found."],
                [TaxonomyErrorCodes.StageNotFound]);
        }

        if (await taxonomyRepository.IsGradeSlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<GradeAdminDto>.Failure(
                ["Grade slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var grade = new Grade(
            request.EducationalStageId,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder);
        await taxonomyRepository.AddGradeAsync(grade, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeAdminDto>.Success(GradeAdminDto.FromGrade(grade));
    }
}
