namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Ambient access to the authenticated request principal for application handlers.
/// Backed by the ASP.NET Core cookie identity; there is no second user store.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
