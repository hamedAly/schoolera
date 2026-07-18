namespace Schoolera.Domain.Enums;

/// <summary>
/// School-level detailed fee visibility policy.
/// Null on <see cref="Entities.School"/> means Product has not resolved the Egypt default —
/// callers must treat that as unresolved, not invent Public or AuthenticatedParentsOnly.
/// </summary>
public enum FeeVisibilityPolicy
{
    Public = 1,
    AuthenticatedParentsOnly = 2,
}
