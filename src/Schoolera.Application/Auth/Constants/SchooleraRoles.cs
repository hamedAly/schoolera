namespace Schoolera.Application.Auth.Constants;

public static class SchooleraRoles
{
    public const string Parent = "Parent";
    public const string SchoolOwner = "SchoolOwner";
    public const string SchoolAdmin = "SchoolAdmin";
    public const string AdmissionOfficer = "AdmissionOfficer";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string ContentModerator = "ContentModerator";
    public const string PlatformAdmin = "PlatformAdmin";
    public const string SupportAgent = "SupportAgent";

    public static readonly IReadOnlyList<string> All =
    [
        Parent,
        SchoolOwner,
        SchoolAdmin,
        AdmissionOfficer,
        FinanceOfficer,
        ContentModerator,
        PlatformAdmin,
        SupportAgent,
    ];

    public static readonly IReadOnlyList<string> PublicRegistration =
    [
        Parent,
        SchoolOwner,
    ];

    public static readonly IReadOnlyList<string> SchoolPortalEntry =
    [
        SchoolOwner,
        SchoolAdmin,
        AdmissionOfficer,
        FinanceOfficer,
        ContentModerator,
    ];
}
