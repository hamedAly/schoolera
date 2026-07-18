namespace Schoolera.Application.SchoolPortal.Auth;

/// <summary>Central school-portal permission identifiers (membership-scoped, not Identity roles).</summary>
public enum SchoolPortalPermission
{
    ViewDashboard = 1,
    ViewTeam = 2,
    ManageTeam = 3,
    TransferOwnership = 4,
    ViewProfile = 5,
    ManageProfile = 6,
    ManageBranches = 7,
    ManageOfferings = 8,
    ManageFacilities = 9,
    ManageGallery = 10,
    ManageServices = 11,
    ManagePublicContact = 12,
    ViewApplications = 13,
    ManageApplicationReview = 14,
    DownloadApplicationAttachments = 15,
    ExportApplications = 16,
    ManageAdmissionRequirements = 17,
    ManageAdmissionQuestions = 18,
    ViewFees = 19,
    ManageFees = 20,
    ManageContent = 21,
}
