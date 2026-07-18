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

namespace Schoolera.Application.Parent.Queries.GetParentChildren;

public sealed record GetParentChildrenQuery : IRequest<Result<IReadOnlyList<ChildProfileListItemDto>>>;

public sealed class GetParentChildrenQueryHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<GetParentChildrenQueryHandler> logger)
    : IRequestHandler<GetParentChildrenQuery, Result<IReadOnlyList<ChildProfileListItemDto>>>
{
    public async Task<Result<IReadOnlyList<ChildProfileListItemDto>>> Handle(
        GetParentChildrenQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<ChildProfileListItemDto>>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        logger.LogInformation("Listing children for parent {UserId}.", userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var children = await childProfileRepository.ListByParentUserIdAsync(
            userId,
            includeInactive: false,
            cancellationToken);

        var items = children.Select(child => new ChildProfileListItemDto(
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
            child.HasSpecialNeeds,
            child.IsActive)).ToArray();

        return Result<IReadOnlyList<ChildProfileListItemDto>>.Success(items);
    }
}
