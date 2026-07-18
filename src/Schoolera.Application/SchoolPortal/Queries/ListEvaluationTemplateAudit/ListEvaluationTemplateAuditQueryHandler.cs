using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListEvaluationTemplateAudit;

public sealed record ListEvaluationTemplateAuditQuery(Guid SchoolId, Guid TemplateId)
    : IRequest<Result<IReadOnlyList<EvaluationTemplateAuditDto>>>;

public sealed class ListEvaluationTemplateAuditQueryHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionEvaluationRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListEvaluationTemplateAuditQuery,
        Result<IReadOnlyList<EvaluationTemplateAuditDto>>>
{
    public async Task<Result<IReadOnlyList<EvaluationTemplateAuditDto>>> Handle(
        ListEvaluationTemplateAuditQuery request, CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null ||
            !accessResult.Data.HasPermission(SchoolPortalPermission.ManageApplicationReview))
            return Result<IReadOnlyList<EvaluationTemplateAuditDto>>.Failure(
                [localizer["AccessDenied"]], [SchoolPortalErrorCodes.AccessDenied]);
        var rows = await repository.ListTemplateAuditAsync(
            request.SchoolId, request.TemplateId, ct);
        return Result<IReadOnlyList<EvaluationTemplateAuditDto>>.Success(
            rows.Select(x => new EvaluationTemplateAuditDto(
                x.Id, x.Action.Split(':', 2)[0], x.Version, x.CreatedAtUtc)).ToArray());
    }
}
