using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolTeam;

public sealed record GetSchoolTeamQuery(Guid SchoolId)
    : IRequest<Result<IReadOnlyList<SchoolTeamMemberDto>>>;

public sealed class GetSchoolTeamQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUserDirectory userDirectory,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolTeamQuery, Result<IReadOnlyList<SchoolTeamMemberDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolTeamMemberDto>>> Handle(
        GetSchoolTeamQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolTeamMemberDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permission = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolTeamMemberDto>>(
            accessResult.Data, SchoolPortalPermission.ViewTeam, localizer);
        if (!permission.Succeeded)
        {
            return permission;
        }

        var school = await repository.GetSchoolProfileAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolTeamMemberDto>>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var team = await SchoolPortalReadModel.BuildTeamAsync(
            school, repository, userDirectory, cancellationToken);

        return Result<IReadOnlyList<SchoolTeamMemberDto>>.Success(team);
    }
}
