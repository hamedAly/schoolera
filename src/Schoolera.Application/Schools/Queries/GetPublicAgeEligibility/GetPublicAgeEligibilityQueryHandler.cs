using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Schools.Queries.GetPublicAgeEligibility;

public sealed record GetPublicAgeEligibilityQuery(
    string Slug,
    Guid? BranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    Guid? ChildProfileId) : IRequest<Result<AgeEligibilityResultDto?>>;

public sealed class GetPublicAgeEligibilityQueryValidator
    : AbstractValidator<GetPublicAgeEligibilityQuery>
{
    public GetPublicAgeEligibilityQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.EducationalStageId).NotEmpty();
        RuleFor(x => x.AcademicYearId).NotEmpty();
    }
}

public sealed class GetPublicAgeEligibilityQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    IChildAgeEligibilityEvaluator evaluator,
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    ILogger<GetPublicAgeEligibilityQueryHandler> logger)
    : IRequestHandler<GetPublicAgeEligibilityQuery, Result<AgeEligibilityResultDto?>>
{
    public async Task<Result<AgeEligibilityResultDto?>> Handle(
        GetPublicAgeEligibilityQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Public age eligibility check for slug {Slug}.", request.Slug);

        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<AgeEligibilityResultDto?>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        DateOnly? birthDate = null;
        if (request.ChildProfileId is { } childId &&
            currentUser.UserId is { } userId)
        {
            var child = await childProfileRepository.GetOwnedAsync(userId, childId, cancellationToken);
            birthDate = child?.BirthDate;
        }

        var evaluation = await evaluator.EvaluateAsync(
            school.Id,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            birthDate,
            cancellationToken);

        return Result<AgeEligibilityResultDto?>.Success(
            AgeEligibilityMapping.FromEvaluation(evaluation));
    }
}
