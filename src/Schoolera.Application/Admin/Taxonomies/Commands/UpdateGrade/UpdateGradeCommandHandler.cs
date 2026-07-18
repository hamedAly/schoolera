using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateGrade;

public sealed record UpdateGradeCommand(
    Guid Id,
    Guid EducationalStageId,
    string NameAr,
    string? NameEn,
    string Slug,
    int SortOrder,
    bool IsActive) : IRequest<Result<GradeAdminDto>>;

public sealed class UpdateGradeCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateGradeCommandHandler> logger)
    : IRequestHandler<UpdateGradeCommand, Result<GradeAdminDto>>
{
    public async Task<Result<GradeAdminDto>> Handle(
        UpdateGradeCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating grade {GradeId}.", request.Id);

        var grade = await taxonomyRepository.GetGradeByIdAsync(request.Id, cancellationToken);
        if (grade is null)
        {
            return Result<GradeAdminDto>.Failure(
                ["Grade not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (!await taxonomyRepository.EducationalStageExistsAsync(
                request.EducationalStageId,
                cancellationToken))
        {
            return Result<GradeAdminDto>.Failure(
                ["Educational stage not found."],
                [TaxonomyErrorCodes.StageNotFound]);
        }

        if (await taxonomyRepository.IsGradeSlugTakenAsync(request.Slug, request.Id, cancellationToken))
        {
            return Result<GradeAdminDto>.Failure(
                ["Grade slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        grade.Update(
            request.EducationalStageId,
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.SortOrder,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeAdminDto>.Success(GradeAdminDto.FromGrade(grade));
    }
}
