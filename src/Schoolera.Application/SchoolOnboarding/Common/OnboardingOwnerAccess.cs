using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Application.SchoolOnboarding.Common;

public static class OnboardingOwnerAccess
{
    /// <summary>
    /// Returns the authenticated SchoolOwner's user id, or null when the caller is not a
    /// SchoolOwner (SchoolAdmin and other roles may not own an onboarding application).
    /// </summary>
    public static Guid? ResolveOwnerId(ICurrentUser currentUser) =>
        currentUser is { IsAuthenticated: true } && currentUser.UserId is { } userId &&
        currentUser.IsInRole(SchooleraRoles.SchoolOwner)
            ? userId
            : null;
}
