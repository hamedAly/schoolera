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

namespace Schoolera.Application.SchoolPortal.Commands.RemoveSchoolAdmin;

public sealed record RemoveSchoolAdminCommand(Guid SchoolId, Guid MembershipId)
    : IRequest<Result<SchoolTeamMemberDto>>;

public sealed class RemoveSchoolAdminCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ISchoolPortalAuditWriter auditWriter,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<RemoveSchoolAdminCommandHandler> logger)
    : IRequestHandler<RemoveSchoolAdminCommand, Result<SchoolTeamMemberDto>>
{
    public async Task<Result<SchoolTeamMemberDto>> Handle(
        RemoveSchoolAdminCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolTeamMemberDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var ownerCheck = SchoolPortalAccess.RequireOwner<SchoolTeamMemberDto>(accessResult.Data, localizer);
        if (!ownerCheck.Succeeded)
        {
            return ownerCheck;
        }

        var editable = SchoolPortalAccess.RequireEditablePermission<SchoolTeamMemberDto>(
            accessResult.Data, SchoolPortalPermission.ManageTeam, localizer);
        if (!editable.Succeeded)
        {
            return editable;
        }

        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var member = await repository.GetTeamMemberForWriteAsync(
            request.SchoolId, request.MembershipId, cancellationToken);
        if (member is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.TeamMemberNotFound);
        }

        if (school.OwnerUserId == member.UserId)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.CannotModifyOwner);
        }

        member.Deactivate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolTeamMemberDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            accessResult.Data.UserId,
            SchoolPortalAuditActions.MemberDeactivated,
            nameof(SchoolTeamMember),
            member.Id.ToString(),
            $"role={member.Role}",
            cancellationToken);

        var users = await userDirectory.GetUsersAsync([member.UserId], cancellationToken);
        users.TryGetValue(member.UserId, out var summary);

        logger.LogInformation(
            "Deactivated school team member {MembershipId} for school {SchoolId}.",
            member.Id,
            request.SchoolId);

        return Result<SchoolTeamMemberDto>.Success(
            SchoolPortalReadModel.ToTeamMemberDto(member, summary));
    }
}
