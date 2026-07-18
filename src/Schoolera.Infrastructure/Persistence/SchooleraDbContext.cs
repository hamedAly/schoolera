using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence;

public sealed class SchooleraDbContext(DbContextOptions<SchooleraDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<School> Schools => Set<School>();

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<Governorate> Governorates => Set<Governorate>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<District> Districts => Set<District>();

    public DbSet<Curriculum> Curricula => Set<Curriculum>();

    public DbSet<EducationalStage> EducationalStages => Set<EducationalStage>();

    public DbSet<Grade> Grades => Set<Grade>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();

    public DbSet<SchoolBranch> SchoolBranches => Set<SchoolBranch>();

    public DbSet<SchoolCurriculum> SchoolCurricula => Set<SchoolCurriculum>();

    public DbSet<SchoolStageOffering> SchoolStageOfferings => Set<SchoolStageOffering>();

    public DbSet<SchoolGradeOffering> SchoolGradeOfferings => Set<SchoolGradeOffering>();

    public DbSet<TuitionFee> TuitionFees => Set<TuitionFee>();

    public DbSet<SchoolFeeInstallmentDisplay> SchoolFeeInstallmentDisplays =>
        Set<SchoolFeeInstallmentDisplay>();

    public DbSet<SchoolPublishedDiscount> SchoolPublishedDiscounts => Set<SchoolPublishedDiscount>();

    public DbSet<SchoolFinancialNote> SchoolFinancialNotes => Set<SchoolFinancialNote>();

    public DbSet<SchoolFacility> SchoolFacilities => Set<SchoolFacility>();

    public DbSet<SchoolImage> SchoolImages => Set<SchoolImage>();

    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public DbSet<SchoolOnboardingApplication> SchoolOnboardingApplications =>
        Set<SchoolOnboardingApplication>();

    public DbSet<SchoolOnboardingDocumentType> SchoolOnboardingDocumentTypes =>
        Set<SchoolOnboardingDocumentType>();

    public DbSet<SchoolOnboardingDocument> SchoolOnboardingDocuments =>
        Set<SchoolOnboardingDocument>();

    public DbSet<SchoolOnboardingStatusHistory> SchoolOnboardingStatusHistory =>
        Set<SchoolOnboardingStatusHistory>();

    public DbSet<SchoolTeamMember> SchoolTeamMembers => Set<SchoolTeamMember>();

    public DbSet<SchoolTeamMemberBranch> SchoolTeamMemberBranches => Set<SchoolTeamMemberBranch>();

    public DbSet<SchoolAdditionalService> SchoolAdditionalServices => Set<SchoolAdditionalService>();

    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();

    public DbSet<SchoolContactLead> SchoolContactLeads => Set<SchoolContactLead>();

    public DbSet<SchoolProfileViewDaily> SchoolProfileViewDaily => Set<SchoolProfileViewDaily>();

    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    public DbSet<ChildDocument> ChildDocuments => Set<ChildDocument>();

    public DbSet<LegalDocumentVersion> LegalDocumentVersions => Set<LegalDocumentVersion>();

    public DbSet<LegalAcceptance> LegalAcceptances => Set<LegalAcceptance>();

    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();

    public DbSet<AdmissionApplicationAttachment> AdmissionApplicationAttachments =>
        Set<AdmissionApplicationAttachment>();

    public DbSet<AdmissionApplicationHistory> AdmissionApplicationHistory =>
        Set<AdmissionApplicationHistory>();

    public DbSet<AdmissionApplicationNumberSequence> AdmissionApplicationNumberSequences =>
        Set<AdmissionApplicationNumberSequence>();

    public DbSet<SchoolAdmissionRequirement> SchoolAdmissionRequirements =>
        Set<SchoolAdmissionRequirement>();

    public DbSet<AdmissionApplicationRequirementSnapshot> AdmissionApplicationRequirementSnapshots =>
        Set<AdmissionApplicationRequirementSnapshot>();

    public DbSet<SchoolAdmissionRequirementAudit> SchoolAdmissionRequirementAudits =>
        Set<SchoolAdmissionRequirementAudit>();

    public DbSet<SchoolInterviewAssessmentPolicy> SchoolInterviewAssessmentPolicies =>
        Set<SchoolInterviewAssessmentPolicy>();

    public DbSet<AdmissionApplicationInterviewAssessmentPolicySnapshot>
        AdmissionApplicationInterviewAssessmentPolicySnapshots =>
        Set<AdmissionApplicationInterviewAssessmentPolicySnapshot>();

    public DbSet<SchoolInterviewAssessmentPolicyAudit> SchoolInterviewAssessmentPolicyAudits =>
        Set<SchoolInterviewAssessmentPolicyAudit>();

    public DbSet<SchoolChildAgeEligibilityRule> SchoolChildAgeEligibilityRules =>
        Set<SchoolChildAgeEligibilityRule>();

    public DbSet<AdmissionApplicationChildAgeEligibilitySnapshot>
        AdmissionApplicationChildAgeEligibilitySnapshots =>
        Set<AdmissionApplicationChildAgeEligibilitySnapshot>();

    public DbSet<SchoolChildAgeEligibilityRuleAudit> SchoolChildAgeEligibilityRuleAudits =>
        Set<SchoolChildAgeEligibilityRuleAudit>();

    public DbSet<SchoolAdmissionQuestion> SchoolAdmissionQuestions => Set<SchoolAdmissionQuestion>();

    public DbSet<SchoolAdmissionQuestionOption> SchoolAdmissionQuestionOptions =>
        Set<SchoolAdmissionQuestionOption>();

    public DbSet<SchoolAdmissionQuestionAudit> SchoolAdmissionQuestionAudits =>
        Set<SchoolAdmissionQuestionAudit>();

    public DbSet<AdmissionApplicationQuestionSnapshot> AdmissionApplicationQuestionSnapshots =>
        Set<AdmissionApplicationQuestionSnapshot>();

    public DbSet<AdmissionApplicationQuestionSnapshotOption> AdmissionApplicationQuestionSnapshotOptions =>
        Set<AdmissionApplicationQuestionSnapshotOption>();

    public DbSet<AdmissionApplicationAnswer> AdmissionApplicationAnswers =>
        Set<AdmissionApplicationAnswer>();

    public DbSet<AdmissionMissingItemsRequest> AdmissionMissingItemsRequests =>
        Set<AdmissionMissingItemsRequest>();

    public DbSet<AdmissionMissingItem> AdmissionMissingItems => Set<AdmissionMissingItem>();

    public DbSet<AdmissionInterviewAppointment> AdmissionInterviewAppointments =>
        Set<AdmissionInterviewAppointment>();

    public DbSet<AdmissionAssessmentAppointment> AdmissionAssessmentAppointments =>
        Set<AdmissionAssessmentAppointment>();

    public DbSet<AdmissionAppointmentActionHistory> AdmissionAppointmentActionHistory =>
        Set<AdmissionAppointmentActionHistory>();

    public DbSet<SchoolAdmissionEvaluationTemplate> SchoolAdmissionEvaluationTemplates =>
        Set<SchoolAdmissionEvaluationTemplate>();

    public DbSet<SchoolAdmissionEvaluationCriterion> SchoolAdmissionEvaluationCriteria =>
        Set<SchoolAdmissionEvaluationCriterion>();

    public DbSet<SchoolAdmissionEvaluationCriterionOption> SchoolAdmissionEvaluationCriterionOptions =>
        Set<SchoolAdmissionEvaluationCriterionOption>();

    public DbSet<SchoolAdmissionEvaluationTemplateAudit> SchoolAdmissionEvaluationTemplateAudits =>
        Set<SchoolAdmissionEvaluationTemplateAudit>();

    public DbSet<AdmissionEvaluationResult> AdmissionEvaluationResults =>
        Set<AdmissionEvaluationResult>();

    public DbSet<AdmissionEvaluationResultVersion> AdmissionEvaluationResultVersions =>
        Set<AdmissionEvaluationResultVersion>();

    public DbSet<AdmissionEvaluationHistory> AdmissionEvaluationHistory =>
        Set<AdmissionEvaluationHistory>();

    public DbSet<AdmissionMeetingSession> AdmissionMeetingSessions =>
        Set<AdmissionMeetingSession>();

    public DbSet<AdmissionMeetingSessionHistory> AdmissionMeetingSessionHistory =>
        Set<AdmissionMeetingSessionHistory>();

    public DbSet<InterviewAssessmentSlot> InterviewAssessmentSlots => Set<InterviewAssessmentSlot>();

    public DbSet<InterviewAssessmentSlotAudit> InterviewAssessmentSlotAudits =>
        Set<InterviewAssessmentSlotAudit>();

    public DbSet<InterviewAssessmentSlotGenerationBatch> InterviewAssessmentSlotGenerationBatches =>
        Set<InterviewAssessmentSlotGenerationBatch>();

    public DbSet<CmsPage> CmsPages => Set<CmsPage>();

    public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();

    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    public DbSet<HomepageContent> HomepageContents => Set<HomepageContent>();

    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();

    public DbSet<PlatformIntegrationConfiguration> PlatformIntegrationConfigurations =>
        Set<PlatformIntegrationConfiguration>();

    public DbSet<CourierProviderProfile> CourierProviderProfiles => Set<CourierProviderProfile>();

    public DbSet<CourierService> CourierServices => Set<CourierService>();

    public DbSet<CourierCoverageRule> CourierCoverageRules => Set<CourierCoverageRule>();

    public DbSet<CourierOperatingWindow> CourierOperatingWindows => Set<CourierOperatingWindow>();

    public DbSet<CourierSlaDefinition> CourierSlaDefinitions => Set<CourierSlaDefinition>();

    public DbSet<CourierHealthCheckRecord> CourierHealthCheckRecords =>
        Set<CourierHealthCheckRecord>();

    public DbSet<NotificationOutboxMessage> NotificationOutboxMessages =>
        Set<NotificationOutboxMessage>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<NotificationTemplateVersion> NotificationTemplateVersions =>
        Set<NotificationTemplateVersion>();

    public DbSet<ParentNotificationPreference> ParentNotificationPreferences =>
        Set<ParentNotificationPreference>();

    public DbSet<ParentAdmissionOpenSubscription> ParentAdmissionOpenSubscriptions =>
        Set<ParentAdmissionOpenSubscription>();

    public DbSet<FavoriteSchool> FavoriteSchools => Set<FavoriteSchool>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();

    public DbSet<SupportTicketHistory> SupportTicketHistory => Set<SupportTicketHistory>();

    public DbSet<SupportTicketAttachment> SupportTicketAttachments => Set<SupportTicketAttachment>();

    public DbSet<SupportTicketNumberSequence> SupportTicketNumberSequences =>
        Set<SupportTicketNumberSequence>();

    public DbSet<SchoolPayableItem> SchoolPayableItems => Set<SchoolPayableItem>();

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<PaymentProviderEvent> PaymentProviderEvents => Set<PaymentProviderEvent>();

    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();

    public DbSet<PaymentReconciliationRecord> PaymentReconciliationRecords =>
        Set<PaymentReconciliationRecord>();

    public DbSet<PaymentReferenceSequence> PaymentReferenceSequences =>
        Set<PaymentReferenceSequence>();

    public DbSet<FinancingRequest> FinancingRequests => Set<FinancingRequest>();

    public DbSet<FinancingOffer> FinancingOffers => Set<FinancingOffer>();

    public DbSet<FinancingDecisionEvent> FinancingDecisionEvents => Set<FinancingDecisionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchooleraDbContext).Assembly);
    }
}
