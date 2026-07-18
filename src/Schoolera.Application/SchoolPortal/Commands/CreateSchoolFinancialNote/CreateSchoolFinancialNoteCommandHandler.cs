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
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolFinancialNote;

public sealed record CreateSchoolFinancialNoteCommand(
    Guid SchoolId,
    CreateSchoolFinancialNoteRequest Body) : IRequest<Result<SchoolFinancialNoteDto>>;

public sealed class CreateSchoolFinancialNoteCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolFinancialNoteCommandHandler> logger)
    : IRequestHandler<CreateSchoolFinancialNoteCommand, Result<SchoolFinancialNoteDto>>
{
    public async Task<Result<SchoolFinancialNoteDto>> Handle(
        CreateSchoolFinancialNoteCommand request,
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

        if (string.IsNullOrWhiteSpace(request.Body.TextAr))
        {
            return SchoolPortalResults.FailureForCode<SchoolFinancialNoteDto>(
                localizer, SchoolPortalErrorCodes.InvalidFee);
        }

        var note = new SchoolFinancialNote(
            request.SchoolId,
            request.Body.TextAr,
            request.Body.TextEn,
            request.Body.IsInternal,
            request.Body.SortOrder);

        await repository.AddFinancialNoteAsync(note, cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFinancialNoteDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Created financial note {NoteId} for school {SchoolId}.",
            note.Id,
            request.SchoolId);
        return Result<SchoolFinancialNoteDto>.Success(SchoolPortalReadModel.ToFinancialNote(note));
    }
}
