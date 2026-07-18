using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Fees;

/// <summary>
/// Resolves whether detailed fee amounts may be revealed.
/// Null policy is legacy compatibility only — not an Egypt Product default
/// (see docs/fee-display-policy.md).
/// </summary>
public static class FeeVisibilityResolver
{
    public static bool CanRevealDetailedFees(
        FeeVisibilityPolicy? policy,
        bool isAuthenticatedParent)
    {
        return policy switch
        {
            null => true,
            FeeVisibilityPolicy.Public => true,
            FeeVisibilityPolicy.AuthenticatedParentsOnly => isAuthenticatedParent,
            _ => false,
        };
    }

    public static bool FeesRequireLogin(FeeVisibilityPolicy? policy) =>
        policy == FeeVisibilityPolicy.AuthenticatedParentsOnly;
}
