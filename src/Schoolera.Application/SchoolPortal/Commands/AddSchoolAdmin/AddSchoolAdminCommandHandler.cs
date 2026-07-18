using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.UpsertSchoolTeamMember;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.AddSchoolAdmin;

public sealed record AddSchoolAdminCommand(Guid SchoolId, AddSchoolAdminRequest Body)
    : IRequest<Result<SchoolTeamMemberDto>>;

/// <summary>
/// Backward-compatible wrapper: upserts a SchoolAdmin with AllBranches scope.
/// Persistence is committed by <see cref="UpsertSchoolTeamMemberCommandHandler"/>.
/// </summary>
public sealed class AddSchoolAdminCommandHandler(
    ISender sender,
    IUnitOfWork unitOfWork,
    ILogger<AddSchoolAdminCommandHandler> logger)
    : IRequestHandler<AddSchoolAdminCommand, Result<SchoolTeamMemberDto>>
{
    public async Task<Result<SchoolTeamMemberDto>> Handle(
        AddSchoolAdminCommand request,
        CancellationToken cancellationToken)
    {
        _ = unitOfWork;

        var result = await sender.Send(
            new UpsertSchoolTeamMemberCommand(
                request.SchoolId,
                new UpsertSchoolTeamMemberRequest(
                    request.Body.Email,
                    SchoolTeamRole.SchoolAdmin,
                    SchoolBranchScopeMode.AllBranches,
                    null)),
            cancellationToken);

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Legacy AddSchoolAdmin succeeded for school {SchoolId}.",
                request.SchoolId);
        }

        return result;
    }
}
