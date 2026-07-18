using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolAdmissionQuestion;

public sealed record GetSchoolAdmissionQuestionQuery(
    Guid SchoolId,
    Guid QuestionId) : IRequest<Result<SchoolAdmissionQuestionDetailDto>>;

public sealed class GetSchoolAdmissionQuestionQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolAdmissionQuestionQuery, Result<SchoolAdmissionQuestionDetailDto>>
{
    public async Task<Result<SchoolAdmissionQuestionDetailDto>> Handle(
        GetSchoolAdmissionQuestionQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<SchoolAdmissionQuestionDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        
        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolAdmissionQuestionDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionQuestions, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

var entity = await questionRepository.GetByIdAsync(
            request.SchoolId,
            request.QuestionId,
            cancellationToken);

        return entity is null
            ? SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionNotFound)
            : Result<SchoolAdmissionQuestionDetailDto>.Success(
                SchoolAdmissionQuestionMapping.ToDetail(entity));
    }
}
