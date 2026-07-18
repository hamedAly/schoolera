using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Parent.Queries.GetParentChild;

public sealed record GetParentChildQuery(Guid ChildId) : IRequest<Result<ChildProfileDetailDto>>;

public sealed class GetParentChildQueryHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<GetParentChildQueryHandler> logger)
    : IRequestHandler<GetParentChildQuery, Result<ChildProfileDetailDto>>
{
    public async Task<Result<ChildProfileDetailDto>> Handle(
        GetParentChildQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ChildProfileDetailDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var child = await childProfileRepository.GetOwnedAsync(userId, request.ChildId, cancellationToken);
        if (child is null || !child.IsActive)
        {
            // Same safe 404 for unknown and non-owned children.
            return Result<ChildProfileDetailDto>.Failure(
                ["Child not found."],
                [ParentErrorCodes.ChildNotFound]);
        }

        logger.LogInformation("Loaded child {ChildId} for parent {UserId}.", child.Id, userId);
        return Result<ChildProfileDetailDto>.Success(Map(child, identityProtector));
    }

    internal static ChildProfileDetailDto Map(ChildProfile child, IChildIdentityProtector identityProtector)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new ChildProfileDetailDto(
            child.Id,
            child.FullName,
            identityProtector.Mask(child.IdentityLastFour),
            child.IdentityType,
            child.BirthDate,
            ParentChildAge.ComputeAgeYears(child.BirthDate, today),
            child.Gender,
            child.CurrentGradeId,
            LocalizationDisplayHelper.Pick(child.CurrentGrade.NameAr, child.CurrentGrade.NameEn),
            child.CurrentSchoolName,
            child.PreferredStudyLanguage,
            child.Skills,
            child.Hobbies,
            child.Strengths,
            child.ImprovementAreas,
            child.HasSpecialNeeds,
            child.SpecialNeedsNotes,
            child.HealthNotes,
            child.IsActive);
    }
}
