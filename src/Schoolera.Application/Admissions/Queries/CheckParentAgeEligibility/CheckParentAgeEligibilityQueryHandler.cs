using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admissions.Queries.CheckParentAgeEligibility;

public sealed record ParentAgeEligibilityCheckRequest(
    Guid SchoolId,
    Guid? SchoolBranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    Guid ChildProfileId);

public sealed record CheckParentAgeEligibilityQuery(ParentAgeEligibilityCheckRequest Body)
    : IRequest<Result<AgeEligibilityResultDto>>;

public sealed class CheckParentAgeEligibilityQueryValidator
    : AbstractValidator<CheckParentAgeEligibilityQuery>
{
    public CheckParentAgeEligibilityQueryValidator()
    {
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.SchoolId).NotEmpty();
        RuleFor(x => x.Body.EducationalStageId).NotEmpty();
        RuleFor(x => x.Body.AcademicYearId).NotEmpty();
        RuleFor(x => x.Body.ChildProfileId).NotEmpty();
    }
}

public sealed class CheckParentAgeEligibilityQueryHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IChildAgeEligibilityEvaluator evaluator,
    IStringLocalizer<AuthMessages> localizer)
    : IRequestHandler<CheckParentAgeEligibilityQuery, Result<AgeEligibilityResultDto>>
{
    public async Task<Result<AgeEligibilityResultDto>> Handle(
        CheckParentAgeEligibilityQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AgeEligibilityResultDto>(localizer);
        }

        var body = request.Body;
        var child = await childProfileRepository.GetOwnedAsync(
            userId,
            body.ChildProfileId,
            cancellationToken);
        if (child is null)
        {
            return AdmissionResults.Failure<AgeEligibilityResultDto>(
                "Child profile not found.",
                Application.Admissions.Constants.AdmissionErrorCodes.StudentNotOwned);
        }

        var evaluation = await evaluator.EvaluateAsync(
            body.SchoolId,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            child.BirthDate,
            cancellationToken);

        return Result<AgeEligibilityResultDto>.Success(
            AgeEligibilityMapping.FromEvaluation(evaluation, canRequestAgeException: false));
    }
}
