using MediatR;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListAccessibleSchools;

public sealed record ListAccessibleSchoolsQuery : IRequest<Result<IReadOnlyList<AccessibleSchoolDto>>>;

public sealed class ListAccessibleSchoolsQueryHandler(
    ISchoolPortalRepository repository,
    ISchoolPortalAccess portalAccess,
    ICurrentUser currentUser)
    : IRequestHandler<ListAccessibleSchoolsQuery, Result<IReadOnlyList<AccessibleSchoolDto>>>
{
    public async Task<Result<IReadOnlyList<AccessibleSchoolDto>>> Handle(
        ListAccessibleSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser is not { IsAuthenticated: true, UserId: { } userId })
        {
            return Result<IReadOnlyList<AccessibleSchoolDto>>.Success(Array.Empty<AccessibleSchoolDto>());
        }

        var schools = await repository.ListAccessibleSchoolsAsync(userId, cancellationToken);
        var result = new List<AccessibleSchoolDto>(schools.Count);

        foreach (var row in schools)
        {
            var accessResult = await portalAccess.ResolveAsync(row.Id, cancellationToken);
            SchoolPortalPermissionsDto? permissions = null;
            if (accessResult is { Succeeded: true, Data: { } access })
            {
                permissions = access.PermissionsDto;
            }

            result.Add(SchoolPortalReadModel.ToAccessibleSchool(row, permissions));
        }

        return Result<IReadOnlyList<AccessibleSchoolDto>>.Success(result);
    }
}
