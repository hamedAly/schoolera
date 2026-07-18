namespace Schoolera.Application.Parent.Constants;

/// <summary>Stable Parent API error codes. Frontends branch on these, never on localized text.</summary>
public static class ParentErrorCodes
{
    public const string ProfileNotFound = "parent.profile.notFound";
    public const string InvalidCityDistrict = "parent.profile.invalidCityDistrict";
    public const string ChildNotFound = "parent.child.notFound";
    public const string IdentityAlreadyExists = "parent.child.identityAlreadyExists";
    public const string InvalidIdentity = "parent.child.invalidIdentity";
    public const string InvalidBirthDate = "parent.child.invalidBirthDate";
    public const string InvalidGrade = "parent.child.invalidGrade";
    public const string SpecialNeedsNotesNotAllowed = "parent.child.specialNeedsNotesNotAllowed";
    public const string CannotDeleteReferencedChild = "parent.child.cannotDeleteReferencedChild";
    public const string OwnershipDenied = "parent.child.ownershipDenied";
    public const string InvalidStudyLanguage = "parent.child.invalidStudyLanguage";
    public const string DocumentNotFound = "parent.child.documentNotFound";
    public const string DocumentInvalidType = "parent.child.documentInvalidType";
    public const string DocumentInvalidFile = "parent.child.documentInvalidFile";
    public const string DocumentTooLarge = "parent.child.documentTooLarge";
    public const string DocumentCopyFailed = "parent.child.documentCopyFailed";
    public const string DocumentAlreadyAttached = "parent.child.documentAlreadyAttached";
    public const string DashboardUnavailable = "parent.dashboard.unavailable";
    public const string Forbidden = "parent.forbidden";
    public const string NotificationNotFound = "parent.notificationNotFound";
    public const string SubscriptionNotFound = "parent.subscriptionNotFound";
    public const string DuplicateSubscription = "parent.duplicateSubscription";
    public const string SchoolNotFound = "parent.schoolNotFound";
    public const string PreferenceConcurrency = "parent.preferenceConcurrency";
}
