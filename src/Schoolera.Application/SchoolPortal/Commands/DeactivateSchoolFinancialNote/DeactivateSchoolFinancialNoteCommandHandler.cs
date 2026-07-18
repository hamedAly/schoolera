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

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolFinancialNote;

public sealed record DeactivateSchoolFinancialNoteCommand(
    Guid SchoolId,
    Guid ItemId) : IRequest<Result<SchoolFinancialNoteDto>>;

public sealed class DeactivateSchoolFinancialNoteCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolFinancialNoteCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolFinancialNoteCommand, Result<SchoolFinancialNoteDto>>
{
    public async Task<Result<SchoolFinancialNoteDto>> Handle(
        DeactivateSchoolFinancialNoteCommand request,
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

        var entity = await repository.GetFinancialNoteForWriteAsync(request.SchoolId, request.ItemId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFinancialNoteDto>(
                localizer, SchoolPortalErrorCodes.FinancialNoteNotFound);
        }

        entity.Deactivate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFinancialNoteDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolFinancialNoteDto>.Success(SchoolPortalReadModel.ToFinancialNote(entity));
    }
}
