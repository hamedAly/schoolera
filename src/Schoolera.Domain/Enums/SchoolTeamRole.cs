namespace Schoolera.Domain.Enums;

/// <summary>
/// School-scoped membership role. Ownership is stored on <c>School.OwnerUserId</c>
/// and is not represented as a membership row.
/// </summary>
public enum SchoolTeamRole
{
    SchoolAdmin = 1,
    AdmissionOfficer = 2,
    FinanceOfficer = 3,
    ContentModerator = 4,
}
