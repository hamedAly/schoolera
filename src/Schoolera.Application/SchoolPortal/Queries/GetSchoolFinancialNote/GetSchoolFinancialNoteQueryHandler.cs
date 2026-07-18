using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolFinancialNote;

public sealed record GetSchoolFinancialNoteQuery(Guid SchoolId, Guid NoteId)
    : IRequest<Result<SchoolFinancialNoteDto>>;

public sealed class GetSchoolFinancialNoteQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolFinancialNoteQuery, Result<SchoolFinancialNoteDto>>
{
    public async Task<Result<SchoolFinancialNoteDto>> Handle(
        GetSchoolFinancialNoteQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolFinancialNoteDto>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolFinancialNoteDto>(
            accessResult.Data, SchoolPortalPermission.ViewFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var item = await repository.GetFinancialNoteForWriteAsync(
            request.SchoolId, request.NoteId, cancellationToken);
        if (item is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFinancialNoteDto>(
                localizer, SchoolPortalErrorCodes.FinancialNoteNotFound);
        }

        return Result<SchoolFinancialNoteDto>.Success(SchoolPortalReadModel.ToFinancialNote(item));
    }
}
