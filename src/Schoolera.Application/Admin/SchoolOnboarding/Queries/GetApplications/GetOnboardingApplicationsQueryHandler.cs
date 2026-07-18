using MediatR;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.SchoolOnboarding.Queries.GetApplications;

public sealed record GetOnboardingApplicationsQuery(
    string? Status,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<AdminOnboardingListItemDto>>>;

public sealed class GetOnboardingApplicationsQueryHandler(
    ISchoolOnboardingRepository repository,
    IUserDirectory userDirectory)
    : IRequestHandler<GetOnboardingApplicationsQuery, Result<PagedResult<AdminOnboardingListItemDto>>>
{
    public async Task<Result<PagedResult<AdminOnboardingListItemDto>>> Handle(
        GetOnboardingApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var pageRequest = new PagedRequest(request.PageNumber, request.PageSize);

        SchoolOnboardingStatus? status =
            Enum.TryParse<SchoolOnboardingStatus>(request.Status, ignoreCase: true, out var parsed)
                ? parsed
                : null;

        var (items, totalCount) = await repository.SearchAsync(
            status,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            cancellationToken);

        var ownerIds = items.Select(application => application.OwnerUserId).Distinct().ToArray();
        var owners = await userDirectory.GetUsersAsync(ownerIds, cancellationToken);

        var mapped = items
            .Select(application =>
            {
                var owner = owners.TryGetValue(application.OwnerUserId, out var summary)
                    ? summary
                    : new UserSummary(application.OwnerUserId, string.Empty, string.Empty);

                return new AdminOnboardingListItemDto(
                    application.Id,
                    application.Status.ToString(),
                    application.OwnerUserId,
                    owner.DisplayName,
                    owner.Email,
                    application.OrganizationNameAr,
                    application.SchoolNameAr,
                    application.CreatedAtUtc,
                    application.UpdatedAtUtc,
                    application.SubmittedAtUtc);
            })
            .ToArray();

        return Result<PagedResult<AdminOnboardingListItemDto>>.Success(
            PagedResult<AdminOnboardingListItemDto>.Create(mapped, totalCount, pageRequest));
    }
}
