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
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolTeamMember;

public sealed record UpdateSchoolTeamMemberCommand(
    Guid SchoolId,
    Guid MembershipId,
    UpdateSchoolTeamMemberRequest Body) : IRequest<Result<SchoolTeamMemberDto>>;

public sealed class UpdateSchoolTeamMemberCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ISchoolPortalAuditWriter auditWriter,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolTeamMemberCommandHandler> logger)
    : IRequestHandler<UpdateSchoolTeamMemberCommand, Result<SchoolTeamMemberDto>>
{
    public async Task<Result<SchoolTeamMemberDto>> Handle(
        UpdateSchoolTeamMemberCommand request,
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

        if (!Enum.IsDefined(request.Body.Role) ||
            request.Body.Role is < SchoolTeamRole.SchoolAdmin or > SchoolTeamRole.ContentModerator)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.InvalidTeamRole);
        }

        var scopeResult = SchoolTeamMembershipSupport.NormalizeScope(
            request.Body.Role,
            request.Body.BranchScopeMode,
            request.Body.BranchIds,
            localizer);
        if (!scopeResult.Succeeded || scopeResult.Data == default)
        {
            return Result<SchoolTeamMemberDto>.Failure(scopeResult.Errors, scopeResult.ErrorCodes);
        }

        var (mode, branchIds) = scopeResult.Data;

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

        var roles = await userDirectory.GetRolesAsync(member.UserId, cancellationToken);
        if (!SchoolTeamMembershipSupport.IsEligibleForRole(roles, request.Body.Role))
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.TeamUserNotEligible);
        }

        var previousRole = member.Role;
        var previousScope = member.BranchScopeMode;
        var previousActive = member.IsActive;

        var apply = await SchoolTeamMembershipSupport.ApplyRoleAndScopeAsync(
            member, request.Body.Role, mode, branchIds, repository, localizer, cancellationToken);
        if (!apply.Succeeded)
        {
            return Result<SchoolTeamMemberDto>.Failure(apply.Errors, apply.ErrorCodes);
        }

        if (request.Body.IsActive && !member.IsActive)
        {
            member.Reactivate();
        }
        else if (!request.Body.IsActive && member.IsActive)
        {
            member.Deactivate();
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolTeamMemberDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var users = await userDirectory.GetUsersAsync([member.UserId], cancellationToken);
        users.TryGetValue(member.UserId, out var summary);

        var auditAction = SchoolPortalAuditActions.MemberUpdated;
        if (previousActive != member.IsActive)
        {
            auditAction = member.IsActive
                ? SchoolPortalAuditActions.MemberActivated
                : SchoolPortalAuditActions.MemberDeactivated;
        }
        else if (previousRole != member.Role)
        {
            auditAction = SchoolPortalAuditActions.RoleChanged;
        }
        else if (previousScope != member.BranchScopeMode)
        {
            auditAction = SchoolPortalAuditActions.BranchScopeChanged;
        }

        await auditWriter.WriteAsync(
            accessResult.Data.UserId,
            auditAction,
            nameof(SchoolTeamMember),
            member.Id.ToString(),
            $"role={member.Role};scope={member.BranchScopeMode};active={member.IsActive}",
            cancellationToken);

        logger.LogInformation(
            "Updated school team member {MembershipId} for school {SchoolId}.",
            member.Id,
            request.SchoolId);

        return Result<SchoolTeamMemberDto>.Success(
            SchoolPortalReadModel.ToTeamMemberDto(member, summary));
    }
}
