using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGrade;

public sealed record DeactivateGradeCommand(Guid Id) : IRequest<Result<GradeAdminDto>>;

public sealed class DeactivateGradeCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateGradeCommandHandler> logger)
    : IRequestHandler<DeactivateGradeCommand, Result<GradeAdminDto>>
{
    public async Task<Result<GradeAdminDto>> Handle(
        DeactivateGradeCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating grade {GradeId}.", request.Id);

        var grade = await taxonomyRepository.GetGradeByIdAsync(request.Id, cancellationToken);
        if (grade is null)
        {
            return Result<GradeAdminDto>.Failure(
                ["Grade not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        grade.Update(
            grade.EducationalStageId,
            grade.NameAr,
            grade.NameEn,
            grade.Slug,
            grade.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeAdminDto>.Success(GradeAdminDto.FromGrade(grade));
    }
}
