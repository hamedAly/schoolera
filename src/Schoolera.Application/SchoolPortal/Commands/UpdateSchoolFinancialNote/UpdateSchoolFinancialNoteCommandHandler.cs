using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFinancialNote;

public sealed record UpdateSchoolFinancialNoteCommand(
    Guid SchoolId,
    Guid NoteId,
    UpdateSchoolFinancialNoteRequest Body) : IRequest<Result<SchoolFinancialNoteDto>>;

public sealed class UpdateSchoolFinancialNoteCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolFinancialNoteCommandHandler> logger)
    : IRequestHandler<UpdateSchoolFinancialNoteCommand, Result<SchoolFinancialNoteDto>>
{
    public async Task<Result<SchoolFinancialNoteDto>> Handle(
        UpdateSchoolFinancialNoteCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolFinancialNoteDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolFinancialNoteDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var note = await repository.GetFinancialNoteForWriteAsync(
            request.SchoolId, request.NoteId, cancellationToken);
        if (note is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFinancialNoteDto>(
                localizer, SchoolPortalErrorCodes.FinancialNoteNotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Body.TextAr))
        {
            return SchoolPortalResults.FailureForCode<SchoolFinancialNoteDto>(
                localizer, SchoolPortalErrorCodes.InvalidFee);
        }

        note.Update(
            request.Body.TextAr,
            request.Body.TextEn,
            request.Body.IsInternal,
            request.Body.SortOrder);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFinancialNoteDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolFinancialNoteDto>.Success(SchoolPortalReadModel.ToFinancialNote(note));
    }
}
