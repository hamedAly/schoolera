using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admissions.Queries.GetApplicationQuestions;

public sealed record GetApplicationQuestionsQuery(Guid ApplicationId)
    : IRequest<Result<AdmissionApplicationQuestionsDto>>;

public sealed class GetApplicationQuestionsQueryHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IAdmissionQuestionCompletenessService questionCompletenessService,
    IStringLocalizer<AuthMessages> localizer)
    : IRequestHandler<GetApplicationQuestionsQuery, Result<AdmissionApplicationQuestionsDto>>
{
    public async Task<Result<AdmissionApplicationQuestionsDto>> Handle(
        GetApplicationQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationQuestionsDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationQuestionsDto>();
        }

        if (AdmissionTransitionPolicy.CanParentEdit(application.Status) &&
            !await questionSnapshotService.HasSnapshotsAsync(application.Id, cancellationToken))
        {
            return Result<AdmissionApplicationQuestionsDto>.Success(
                new AdmissionApplicationQuestionsDto(application.Id, []));
        }

        var evaluation = await questionCompletenessService.EvaluateAsync(
            application,
            System.Globalization.CultureInfo.CurrentUICulture.Name,
            cancellationToken);

        return Result<AdmissionApplicationQuestionsDto>.Success(
            new AdmissionApplicationQuestionsDto(
                application.Id,
                AdmissionMapping.ToQuestionChecklist(application, evaluation)));
    }
}
