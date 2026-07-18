using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.GetAdmissionEvaluationResult;

public sealed record GetAdmissionEvaluationResultQuery(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind)
    : IRequest<Result<AdmissionEvaluationSessionContextDto>>
{
    public static GetAdmissionEvaluationResultQuery From(
        Guid schoolId, Guid applicationId, int kind) =>
        new(schoolId, applicationId, (SlotKind)kind);
}

public sealed class GetAdmissionEvaluationResultQueryHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer)
    : IRequestHandler<GetAdmissionEvaluationResultQuery, Result<AdmissionEvaluationSessionContextDto>>
{
    public async Task<Result<AdmissionEvaluationSessionContextDto>> Handle(
        GetAdmissionEvaluationResultQuery request, CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null ||
            !accessResult.Data.HasPermission(SchoolPortalPermission.ManageApplicationReview))
            return NotFound();
        var application = await applications.GetForSchoolAsync(
            request.SchoolId, request.ApplicationId, ct);
        if (application is null || !accessResult.Data.CanAccessBranch(application.SchoolBranchId))
            return NotFound();
        var result = await evaluations.GetContextAsync(
            request.SchoolId, request.ApplicationId, request.Kind, accessResult.Data.UserId, ct);
        if (result is null) return NotFound();
        if (!accessResult.Data.IsOwner &&
            accessResult.Data.MembershipRole != SchoolTeamRole.SchoolAdmin)
            result = result with
            {
                Capabilities = result.Capabilities with { CanBeginCorrection = false },
            };
        return Result<AdmissionEvaluationSessionContextDto>.Success(result);
    }

    private Result<AdmissionEvaluationSessionContextDto> NotFound() =>
        Result<AdmissionEvaluationSessionContextDto>.Failure(
            [localizer["EvaluationNotFound"]], [AdmissionErrorCodes.EvaluationNotFound]);
}
