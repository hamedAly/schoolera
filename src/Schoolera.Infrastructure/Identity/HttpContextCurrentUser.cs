using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Identity;

/// <summary>
/// Resolves the authenticated user from the ambient HTTP context (secure application cookie).
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
