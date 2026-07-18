using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Common;

internal static class SupportTicketAccess
{
    public static bool IsParent(ICurrentUser currentUser) =>
        currentUser.UserId is not null && currentUser.IsInRole(SchooleraRoles.Parent);

    public static bool IsSupportOrAdmin(ICurrentUser currentUser) =>
        currentUser.UserId is not null &&
        (currentUser.IsInRole(SchooleraRoles.SupportAgent) ||
         currentUser.IsInRole(SchooleraRoles.PlatformAdmin));

    public static bool IsPlatformAdmin(ICurrentUser currentUser) =>
        currentUser.UserId is not null && currentUser.IsInRole(SchooleraRoles.PlatformAdmin);

    public static SupportTicketAuthorType ResolveAuthorType(ICurrentUser currentUser)
    {
        if (currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return SupportTicketAuthorType.PlatformAdmin;
        }

        if (currentUser.IsInRole(SchooleraRoles.SupportAgent))
        {
            return SupportTicketAuthorType.SupportAgent;
        }

        return SupportTicketAuthorType.Parent;
    }
}
