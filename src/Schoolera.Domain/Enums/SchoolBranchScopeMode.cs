namespace Schoolera.Domain.Enums;

/// <summary>
/// Branch access mode for AdmissionOfficer and FinanceOfficer memberships.
/// Empty SelectedBranches must never be treated as unrestricted access.
/// </summary>
public enum SchoolBranchScopeMode
{
    AllBranches = 1,
    SelectedBranches = 2,
}
