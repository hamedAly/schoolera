namespace Schoolera.Domain.Common;

/// <summary>
/// Shared field-length limits used by Domain, Application validators, and EF configurations.
/// </summary>
public static class FieldLengthLimits
{
    public const int SchoolName = 200;

    public const int SchoolCity = 100;

    public const int UserFirstName = 100;

    public const int UserLastName = 100;

    public const int UserPhone = 32;

    public const int PreferredLanguage = 8;

    public const int Slug = 200;

    public const int TaxonomyName = 200;

    public const int TaxonomyDescription = 2000;

    public const int Url = 500;

    public const int Email = 256;

    public const int Phone = 32;

    public const int CurrencyCode = 3;

    public const int SeoTitle = 200;

    public const int SeoDescription = 500;

    public const int AddressLine = 300;

    public const int Landmark = 200;

    public const int PostalCode = 20;

    public const int BranchCode = 64;

    public const int IconKey = 64;

    public const int ImageCaption = 300;

    public const int ImageAlt = 200;

    public const int Notes = 1000;

    // School onboarding
    public const int CountryCode = 2;

    public const int OrganizationName = 200;

    public const int LegalName = 200;

    public const int RegistrationNumber = 100;

    public const int TaxNumber = 100;

    public const int LegalForm = 100;

    public const int PersonName = 200;

    public const int JobTitle = 150;

    public const int IdentityReference = 100;

    public const int OnboardingDocumentTypeCode = 64;

    public const int OnboardingFileName = 260;

    public const int OnboardingContentType = 128;

    public const int OnboardingStoredReference = 400;

    public const int Sha256Hex = 64;

    public const int OnboardingReason = 2000;

    // School portal
    public const int ServiceName = 200;

    public const int ServiceDescription = 2000;

    /// <summary>Aligned with existing Schools.ShortDescription* column length.</summary>
    public const int ShortDescription = 2000;

    public const int FullDescription = 8000;

    // Parent / child profiles
    public const int ChildIdentityProtected = 512;

    public const int ChildIdentityLastFour = 4;

    public const int PreferredContactMethod = 32;

    /// <summary>Skills, hobbies, strengths, improvement areas (plain text).</summary>
    public const int ChildFreeText = 1000;

    public const int ChildCurrentSchoolName = 200;

    public const int ChildDocumentType = 64;

    public const int ParentQualification = 200;

    public const int ParentOccupation = 200;

    public const int GuardianMaskedIdentity = 32;

    // Legal consent versions
    public const int LegalCulture = 8;

    public const int LegalTitle = 200;

    public const int LegalContent = 50000;

    // Admission applications
    public const int ApplicationNumber = 32;

    public const int AdmissionParentNotes = 2000;

    public const int AdmissionSchoolNotes = 2000;

    public const int AdmissionRejectionReason = 2000;

    public const int AdmissionCancellationReason = 1000;

    public const int AdmissionSnapshotName = 200;

    public const int AdmissionAttachmentType = 64;

    public const int AdmissionRequirementCode = 64;

    public const int AdmissionRequirementName = 200;

    public const int AdmissionRequirementDescription = 2000;

    public const int AdmissionRequirementAllowedFiles = 128;

    public const int AdmissionRequirementScopeKey = 140;

    public const int AdmissionRequirementAuditAction = 64;

    public const int AdmissionRequirementAuditMetadata = 500;

    public const int InterviewAssessmentPolicyNotes = 2000;

    public const int MeetingProviderCode = 64;

    public const int PolicyScopeKey = 140;

    public const int PolicyAuditAction = 64;

    public const int PolicyAuditMetadata = 500;

    public const int AgeEligibilityExplanation = 2000;

    public const int AgeEligibilityScopeKey = 140;

    public const int AgeEligibilityAuditAction = 64;

    public const int AgeEligibilityAuditMetadata = 500;

    public const int AgeEligibilityExceptionNote = 1000;

    public const int AdmissionQuestionCode = 64;

    public const int AdmissionQuestionLabel = 200;

    public const int AdmissionQuestionHelp = 2000;

    public const int AdmissionQuestionOptionCode = 64;

    public const int AdmissionQuestionOptionLabel = 200;

    public const int AdmissionQuestionShortTextMax = 200;

    public const int AdmissionQuestionLongTextMax = 2000;

    public const int AdmissionQuestionMaxOptions = 20;

    public const int AdmissionQuestionMaxMultiSelect = 10;

    public const int AdmissionQuestionAllowedFiles = 128;

    public const int AdmissionQuestionAnswerText = 2000;

    public const int AdmissionQuestionSelectedOptions = 640;

    public const int AdmissionQuestionAuditAction = 64;

    public const int AdmissionQuestionAuditMetadata = 500;

    public const int AdmissionHistoryAction = 64;

    public const int AdmissionHistoryNote = 2000;

    public const int AdmissionActorRole = 64;

    // CMS / FAQ / Homepage / Contact
    public const int CmsTitle = 200;

    public const int CmsContent = 50000;

    public const int CmsMetaTitle = 200;

    public const int CmsMetaDescription = 500;

    public const int FaqQuestion = 500;

    public const int FaqAnswer = 10000;

    public const int HomepageHeroTitle = 200;

    public const int HomepageHeroSubtitle = 1000;

    public const int HomepageCtaLabel = 100;

    public const int HomepageSectionTitle = 200;

    public const int HomepageSectionText = 4000;

    public const int ContactSubject = 200;

    public const int ContactMessage = 4000;

    public const int ContactCategory = 64;

    public const int ContactSource = 64;

    public const int ContactReference = 32;

    public const int ContactAdminNote = 2000;

    public const int ContactHoneypot = 200;

    // Support tickets
    public const int SupportTicketReference = 32;

    public const int SupportTicketSubject = 200;

    public const int SupportTicketMessageBody = 4000;

    public const int SupportTicketHistoryValue = 100;

    public const int SupportTicketHistorySummary = 300;

    public const int SupportTicketFileName = 260;

    public const int SupportTicketContentType = 128;

    public const int SupportTicketStorageKey = 400;

    // Payments and financing
    public const int PaymentReference = 40;

    public const int PaymentIdempotencyKey = 128;

    public const int PaymentProviderCode = 64;

    public const int PaymentProviderReference = 128;

    public const int PaymentCurrencyCode = 3;

    public const int PaymentRedirectUrl = 2000;

    public const int PaymentFailureCode = 64;

    public const int PaymentReceiptNumber = 40;

    public const int PaymentInstructions = 2000;

    public const int PaymentEventType = 64;

    public const int PaymentSafeSummary = 500;

    public const int PaymentReconciliationNote = 1000;

    public const int FinancingDisclosure = 4000;

    // Courier configuration
    public const int CourierCode = 64;

    public const int CourierDescription = 2000;

    public const int CourierLogoReference = 500;

    public const int CourierScopeKey = 180;

    public const int CourierTimeZone = 100;

    public const int CourierSafeCode = 100;
}
