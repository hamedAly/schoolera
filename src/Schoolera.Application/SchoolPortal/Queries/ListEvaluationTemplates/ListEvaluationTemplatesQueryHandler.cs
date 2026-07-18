using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListEvaluationTemplates;

public sealed record ListEvaluationTemplatesQuery(
    Guid SchoolId, EvaluationTemplateListQuery Filter)
    : IRequest<Result<IReadOnlyList<EvaluationTemplateDto>>>
{
    public static ListEvaluationTemplatesQuery From(
        Guid schoolId, Guid? branchId, Guid? stageId, Guid? gradeId, Guid? academicYearId,
        int? kind, int? publicationStatus, bool? isActive) =>
        new(schoolId, new(
            branchId, stageId, gradeId, academicYearId,
            kind.HasValue ? (Schoolera.Domain.Enums.EvaluationTemplateKind?)kind.Value : null,
            publicationStatus.HasValue
                ? (Schoolera.Domain.Enums.EvaluationTemplatePublicationStatus?)publicationStatus.Value
                : null,
            isActive));
}

public sealed class ListEvaluationTemplatesQueryHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionEvaluationRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListEvaluationTemplatesQuery, Result<IReadOnlyList<EvaluationTemplateDto>>>
{
    public async Task<Result<IReadOnlyList<EvaluationTemplateDto>>> Handle(
        ListEvaluationTemplatesQuery request, CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<IReadOnlyList<EvaluationTemplateDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        var access = accessResult.Data;
        if (!access.HasPermission(SchoolPortalPermission.ManageApplicationReview))
            return Result<IReadOnlyList<EvaluationTemplateDto>>.Failure(
                [localizer["AccessDenied"]], [SchoolPortalErrorCodes.AccessDenied]);
        var templates = await repository.ListTemplatesAsync(request.SchoolId, request.Filter, ct);
        if (!access.AllowsAllBranches)
            templates = templates.Where(x =>
                !x.SchoolBranchId.HasValue || access.CanAccessBranch(x.SchoolBranchId.Value)).ToArray();
        return Result<IReadOnlyList<EvaluationTemplateDto>>.Success(
            templates.Select(AdmissionEvaluationSupport.MapTemplate).ToArray());
    }
}
