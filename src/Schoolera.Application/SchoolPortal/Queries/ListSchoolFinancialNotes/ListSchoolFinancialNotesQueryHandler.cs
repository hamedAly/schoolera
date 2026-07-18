using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolFinancialNotes;

public sealed record ListSchoolFinancialNotesQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolFinancialNoteDto>>>;

public sealed class ListSchoolFinancialNotesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolFinancialNotesQuery, Result<IReadOnlyList<SchoolFinancialNoteDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolFinancialNoteDto>>> Handle(
        ListSchoolFinancialNotesQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolFinancialNoteDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolFinancialNoteDto>>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var items = await repository.ListFinancialNotesAsync(request.SchoolId, cancellationToken);
        return Result<IReadOnlyList<SchoolFinancialNoteDto>>.Success(
            items.Select(SchoolPortalReadModel.ToFinancialNote).ToArray());
    }
}
