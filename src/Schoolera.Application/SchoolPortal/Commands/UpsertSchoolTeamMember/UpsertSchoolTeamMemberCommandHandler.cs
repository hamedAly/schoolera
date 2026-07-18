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

namespace Schoolera.Application.SchoolPortal.Commands.UpsertSchoolTeamMember;

public sealed record UpsertSchoolTeamMemberCommand(Guid SchoolId, UpsertSchoolTeamMemberRequest Body)
    : IRequest<Result<SchoolTeamMemberDto>>;

public sealed class UpsertSchoolTeamMemberCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ISchoolPortalAuditWriter auditWriter,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpsertSchoolTeamMemberCommandHandler> logger)
    : IRequestHandler<UpsertSchoolTeamMemberCommand, Result<SchoolTeamMemberDto>>
{
    public async Task<Result<SchoolTeamMemberDto>> Handle(
        UpsertSchoolTeamMemberCommand request,
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

        var user = await userDirectory.FindByEmailAsync(request.Body.Email, cancellationToken);
        if (user is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.UserNotFound);
        }

        if (school.OwnerUserId == user.Id)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.CannotModifyOwner);
        }

        var roles = await userDirectory.GetRolesAsync(user.Id, cancellationToken);
        if (!SchoolTeamMembershipSupport.IsEligibleForRole(roles, request.Body.Role))
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.TeamUserNotEligible);
        }

        var existing = await repository.GetTeamMemberByUserForWriteAsync(
            request.SchoolId, user.Id, cancellationToken);

        SchoolTeamMember member;
        string auditAction;
        if (existing is null)
        {
            member = new SchoolTeamMember(
                request.SchoolId,
                user.Id,
                request.Body.Role,
                accessResult.Data.UserId,
                mode);
            school.AddTeamMember(member);

            var apply = await SchoolTeamMembershipSupport.ApplyRoleAndScopeAsync(
                member, request.Body.Role, mode, branchIds, repository, localizer, cancellationToken);
            if (!apply.Succeeded)
            {
                return Result<SchoolTeamMemberDto>.Failure(apply.Errors, apply.ErrorCodes);
            }

            auditAction = SchoolPortalAuditActions.MemberAdded;
        }
        else if (!existing.IsActive)
        {
            existing.Reactivate();
            var apply = await SchoolTeamMembershipSupport.ApplyRoleAndScopeAsync(
                existing, request.Body.Role, mode, branchIds, repository, localizer, cancellationToken);
            if (!apply.Succeeded)
            {
                return Result<SchoolTeamMemberDto>.Failure(apply.Errors, apply.ErrorCodes);
            }

            member = existing;
            auditAction = SchoolPortalAuditActions.MemberActivated;
        }
        else
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.TeamMemberAlreadyExists);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolTeamMemberDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            accessResult.Data.UserId,
            auditAction,
            nameof(SchoolTeamMember),
            member.Id.ToString(),
            $"role={member.Role};scope={member.BranchScopeMode};user={user.Email}",
            cancellationToken);

        logger.LogInformation(
            "Upserted school team member {MembershipId} for school {SchoolId} as {Role}.",
            member.Id,
            request.SchoolId,
            member.Role);

        return Result<SchoolTeamMemberDto>.Success(
            SchoolPortalReadModel.ToTeamMemberDto(member, user));
    }
}
