using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>Builds the PlatformAdmin review projection (shared owner data + admin-only data).</summary>
public static class OnboardingAdminReadModel
{
    public static async Task<AdminOnboardingDetailDto> BuildAsync(
        SchoolOnboardingApplication application,
        ISchoolOnboardingRepository repository,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        var shared = await OnboardingReadModel.BuildAsync(application, repository, cancellationToken);
        var users = await userDirectory.GetUsersAsync([application.OwnerUserId], cancellationToken);
        var owner = users.TryGetValue(application.OwnerUserId, out var summary)
            ? summary
            : new UserSummary(application.OwnerUserId, string.Empty, string.Empty);

        return AdminOnboardingDetailDto.Create(application, shared, owner);
    }
}
