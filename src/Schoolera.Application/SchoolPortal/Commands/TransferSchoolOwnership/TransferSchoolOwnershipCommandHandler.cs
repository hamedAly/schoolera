using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.TransferSchoolOwnership;

public sealed record TransferSchoolOwnershipCommand(
    Guid SchoolId,
    TransferSchoolOwnershipRequest Body) : IRequest<Result<SchoolTeamMemberDto>>;

public sealed class TransferSchoolOwnershipCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ISchoolPortalAuditWriter auditWriter,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<TransferSchoolOwnershipCommandHandler> logger)
    : IRequestHandler<TransferSchoolOwnershipCommand, Result<SchoolTeamMemberDto>>
{
    public async Task<Result<SchoolTeamMemberDto>> Handle(
        TransferSchoolOwnershipCommand request,
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
            accessResult.Data, SchoolPortalPermission.TransferOwnership, localizer);
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

        var newOwner = await userDirectory.FindByEmailAsync(request.Body.NewOwnerEmail, cancellationToken);
        if (newOwner is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.UserNotFound);
        }

        if (school.OwnerUserId == newOwner.Id)
        {
            var users = await userDirectory.GetUsersAsync([newOwner.Id], cancellationToken);
            users.TryGetValue(newOwner.Id, out var selfSummary);
            return Result<SchoolTeamMemberDto>.Success(new SchoolTeamMemberDto(
                MembershipId: null,
                newOwner.Id,
                selfSummary?.DisplayName ?? newOwner.DisplayName,
                selfSummary?.Email ?? newOwner.Email,
                IsOwner: true,
                Role: null,
                IsActive: true,
                BranchScopeMode: Domain.Enums.SchoolBranchScopeMode.AllBranches,
                AllowedBranchIds: Array.Empty<Guid>(),
                JoinedAtUtc: null));
        }

        var roles = await userDirectory.GetRolesAsync(newOwner.Id, cancellationToken);
        if (!roles.Contains(SchooleraRoles.SchoolOwner))
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.OwnershipTransferInvalid);
        }

        if (roles.Contains(SchooleraRoles.Parent) ||
            roles.Contains(SchooleraRoles.PlatformAdmin) ||
            roles.Contains(SchooleraRoles.SupportAgent))
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.OwnershipTransferInvalid);
        }

        var previousOwnerId = school.OwnerUserId;
        school.AssignOwner(newOwner.Id);

        if (school.OwnerUserId is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolTeamMemberDto>(
                localizer, SchoolPortalErrorCodes.LastOwnerProtected);
        }

        // Deactivate any active team membership for the new owner (ownership is not duplicated as membership).
        var newOwnerMembership = await repository.GetTeamMemberByUserForWriteAsync(
            request.SchoolId, newOwner.Id, cancellationToken);
        if (newOwnerMembership is { IsActive: true })
        {
            newOwnerMembership.Deactivate();
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolTeamMemberDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            accessResult.Data.UserId,
            SchoolPortalAuditActions.OwnershipTransferred,
            nameof(School),
            school.Id.ToString(),
            $"from={previousOwnerId};to={newOwner.Id};email={newOwner.Email}",
            cancellationToken);

        logger.LogInformation(
            "Transferred ownership of school {SchoolId} from {PreviousOwnerId} to {NewOwnerId}.",
            school.Id,
            previousOwnerId,
            newOwner.Id);

        return Result<SchoolTeamMemberDto>.Success(new SchoolTeamMemberDto(
            MembershipId: null,
            newOwner.Id,
            newOwner.DisplayName,
            newOwner.Email,
            IsOwner: true,
            Role: null,
            IsActive: true,
            BranchScopeMode: Domain.Enums.SchoolBranchScopeMode.AllBranches,
            AllowedBranchIds: Array.Empty<Guid>(),
            JoinedAtUtc: null));
    }
}
