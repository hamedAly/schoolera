using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolInterviewFaq;

public sealed record GetSchoolInterviewFaqQuery(Guid SchoolId, Guid ItemId)
    : IRequest<Result<SchoolInterviewFaqDetailDto>>;

public sealed class GetSchoolInterviewFaqQueryHandler(
    ISchoolPortalAccess portalAccess,
    ICmsRepository cmsRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolInterviewFaqQuery, Result<SchoolInterviewFaqDetailDto>>
{
    public async Task<Result<SchoolInterviewFaqDetailDto>> Handle(
        GetSchoolInterviewFaqQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<SchoolInterviewFaqDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolInterviewFaqDetailDto>(
            access.Data, SchoolPortalPermission.ManageContent, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var item = await cmsRepository.GetSchoolFaqItemAsync(request.SchoolId, request.ItemId, cancellationToken);
        if (item is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewFaqNotFound);
        }

        return Result<SchoolInterviewFaqDetailDto>.Success(SchoolInterviewFaqSupport.ToDetail(item));
    }
}
