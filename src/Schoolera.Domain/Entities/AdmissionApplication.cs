using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Parent-owned school admission application. The student is the existing <see cref="ChildProfile"/>.
/// School review fields (SchoolNotes, RejectionReason) are internal unless explicitly marked Parent-visible.
/// </summary>
public sealed class AdmissionApplication
{
    private AdmissionApplication()
    {
    }

    public AdmissionApplication(
        string applicationNumber,
        Guid parentUserId,
        Guid parentProfileId,
        Guid childProfileId,
        Guid schoolId,
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid gradeId,
        Guid academicYearId,
        string? parentNotes)
    {
        Id = Guid.NewGuid();
        ApplicationNumber = applicationNumber;
        ParentUserId = parentUserId;
        ParentProfileId = parentProfileId;
        ChildProfileId = childProfileId;
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Status = AdmissionApplicationStatus.Draft;
        ParentNotes = NormalizeOptional(parentNotes);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Attachments = new List<AdmissionApplicationAttachment>();
        History = new List<AdmissionApplicationHistory>();
        RequirementSnapshots = new List<AdmissionApplicationRequirementSnapshot>();
        QuestionSnapshots = new List<AdmissionApplicationQuestionSnapshot>();
        Answers = new List<AdmissionApplicationAnswer>();
        MissingItemsRequests = new List<AdmissionMissingItemsRequest>();
        InterviewAppointments = new List<AdmissionInterviewAppointment>();
        AssessmentAppointments = new List<AdmissionAssessmentAppointment>();
        PolicySnapshot = null;
        AgeEligibilitySnapshot = null;
    }

    public Guid Id { get; private set; }

    public string ApplicationNumber { get; private set; } = string.Empty;

    public Guid ParentUserId { get; private set; }

    public Guid ParentProfileId { get; private set; }

    public ParentProfile ParentProfile { get; private set; } = null!;

    public Guid ChildProfileId { get; private set; }

    public ChildProfile ChildProfile { get; private set; } = null!;

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public Guid SchoolBranchId { get; private set; }

    public SchoolBranch SchoolBranch { get; private set; } = null!;

    public Guid EducationalStageId { get; private set; }

    public EducationalStage EducationalStage { get; private set; } = null!;

    public Guid GradeId { get; private set; }

    public Grade Grade { get; private set; } = null!;

    public Guid AcademicYearId { get; private set; }

    public AcademicYear AcademicYear { get; private set; } = null!;

    public AdmissionApplicationStatus Status { get; private set; }

    public string? ParentNotes { get; private set; }

    /// <summary>Internal / school-facing notes. Never expose on Parent APIs.</summary>
    public string? SchoolNotes { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? ReviewStartedAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public DateTimeOffset? RejectedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? RegisteredAtUtc { get; private set; }

    public DateTimeOffset? WaitingListEnteredAtUtc { get; private set; }

    /// <summary>Parent-visible waiting-list reason. Never fabricate a queue position.</summary>
    public string? WaitingListReason { get; private set; }

    public int? WaitingListPosition { get; private set; }

    public DateOnly? WaitingListReviewDate { get; private set; }

    /// <summary>Internal rejection detail. Parent-visible copy lives only on history when ParentVisible.</summary>
    public string? RejectionReason { get; private set; }

    public string? CancellationReason { get; private set; }

    // Submission snapshots (bounded display fields)
    public string? SubmittedChildFullName { get; private set; }

    public string? SubmittedChildCurrentSchoolName { get; private set; }

    public int? SubmittedChildPreferredStudyLanguage { get; private set; }

    public string? SubmittedChildSkills { get; private set; }

    public string? SubmittedChildHobbies { get; private set; }

    public string? SubmittedChildStrengths { get; private set; }

    public string? SubmittedChildImprovementAreas { get; private set; }

    public bool? SubmittedChildHasSpecialNeeds { get; private set; }

    public string? SubmittedChildSpecialNeedsNotes { get; private set; }

    public string? SubmittedSchoolNameAr { get; private set; }

    public string? SubmittedSchoolNameEn { get; private set; }

    public string? SubmittedBranchNameAr { get; private set; }

    public string? SubmittedBranchNameEn { get; private set; }

    public string? SubmittedStageNameAr { get; private set; }

    public string? SubmittedStageNameEn { get; private set; }

    public string? SubmittedGradeNameAr { get; private set; }

    public string? SubmittedGradeNameEn { get; private set; }

    public string? SubmittedAcademicYearNameAr { get; private set; }

    public string? SubmittedAcademicYearNameEn { get; private set; }

    public string? SubmittedParentDisplayName { get; private set; }

    public string? SubmittedParentEmail { get; private set; }

    public string? SubmittedParentPhone { get; private set; }

    public string? SubmittedParentAlternatePhone { get; private set; }

    public string? SubmittedFatherFullName { get; private set; }

    public string? SubmittedFatherPhone { get; private set; }

    public string? SubmittedFatherEmail { get; private set; }

    public string? SubmittedFatherOccupation { get; private set; }

    public string? SubmittedFatherQualification { get; private set; }

    public string? SubmittedFatherMaskedIdentity { get; private set; }

    public string? SubmittedMotherFullName { get; private set; }

    public string? SubmittedMotherPhone { get; private set; }

    public string? SubmittedMotherEmail { get; private set; }

    public string? SubmittedMotherOccupation { get; private set; }

    public string? SubmittedMotherQualification { get; private set; }

    public string? SubmittedMotherMaskedIdentity { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public ICollection<AdmissionApplicationAttachment> Attachments { get; private set; } = null!;

    public ICollection<AdmissionApplicationHistory> History { get; private set; } = null!;

    public ICollection<AdmissionApplicationRequirementSnapshot> RequirementSnapshots { get; private set; } = null!;

    public ICollection<AdmissionApplicationQuestionSnapshot> QuestionSnapshots { get; private set; } = null!;

    public ICollection<AdmissionApplicationAnswer> Answers { get; private set; } = null!;

    public ICollection<AdmissionMissingItemsRequest> MissingItemsRequests { get; private set; } = null!;

    public ICollection<AdmissionInterviewAppointment> InterviewAppointments { get; private set; } = null!;

    public ICollection<AdmissionAssessmentAppointment> AssessmentAppointments { get; private set; } = null!;

    /// <summary>Optional one-to-one interview/assessment policy snapshot for this application.</summary>
    public AdmissionApplicationInterviewAssessmentPolicySnapshot? PolicySnapshot { get; private set; }

    /// <summary>Optional one-to-one child age eligibility snapshot for this application.</summary>
    public AdmissionApplicationChildAgeEligibilitySnapshot? AgeEligibilitySnapshot { get; private set; }

    public bool IsDraft => Status == AdmissionApplicationStatus.Draft;

    public AdmissionMissingItemsRequest? ActiveMissingItemsRequest =>
        MissingItemsRequests?.FirstOrDefault(request => request.IsActive);

    public bool HasSubmissionSnapshot => SubmittedAtUtc is not null;

    public void UpdateDraftSelection(
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid gradeId,
        Guid academicYearId,
        string? parentNotes)
    {
        EnsureDraft();
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ParentNotes = NormalizeOptional(parentNotes);
        Touch();
    }

    public void Submit(AdmissionSubmissionSnapshot snapshot)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(snapshot);

        Status = AdmissionApplicationStatus.Submitted;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
        SubmittedChildFullName = snapshot.ChildFullName;
        SubmittedChildCurrentSchoolName = snapshot.ChildCurrentSchoolName;
        SubmittedChildPreferredStudyLanguage = snapshot.ChildPreferredStudyLanguage;
        SubmittedChildSkills = snapshot.ChildSkills;
        SubmittedChildHobbies = snapshot.ChildHobbies;
        SubmittedChildStrengths = snapshot.ChildStrengths;
        SubmittedChildImprovementAreas = snapshot.ChildImprovementAreas;
        SubmittedChildHasSpecialNeeds = snapshot.ChildHasSpecialNeeds;
        SubmittedChildSpecialNeedsNotes = snapshot.ChildSpecialNeedsNotes;
        SubmittedSchoolNameAr = snapshot.SchoolNameAr;
        SubmittedSchoolNameEn = snapshot.SchoolNameEn;
        SubmittedBranchNameAr = snapshot.BranchNameAr;
        SubmittedBranchNameEn = snapshot.BranchNameEn;
        SubmittedStageNameAr = snapshot.StageNameAr;
        SubmittedStageNameEn = snapshot.StageNameEn;
        SubmittedGradeNameAr = snapshot.GradeNameAr;
        SubmittedGradeNameEn = snapshot.GradeNameEn;
        SubmittedAcademicYearNameAr = snapshot.AcademicYearNameAr;
        SubmittedAcademicYearNameEn = snapshot.AcademicYearNameEn;
        SubmittedParentDisplayName = snapshot.ParentDisplayName;
        SubmittedParentEmail = snapshot.ParentEmail;
        SubmittedParentPhone = snapshot.ParentPhone;
        SubmittedParentAlternatePhone = snapshot.ParentAlternatePhone;
        SubmittedFatherFullName = snapshot.FatherFullName;
        SubmittedFatherPhone = snapshot.FatherPhone;
        SubmittedFatherEmail = snapshot.FatherEmail;
        SubmittedFatherOccupation = snapshot.FatherOccupation;
        SubmittedFatherQualification = snapshot.FatherQualification;
        SubmittedFatherMaskedIdentity = snapshot.FatherMaskedIdentity;
        SubmittedMotherFullName = snapshot.MotherFullName;
        SubmittedMotherPhone = snapshot.MotherPhone;
        SubmittedMotherEmail = snapshot.MotherEmail;
        SubmittedMotherOccupation = snapshot.MotherOccupation;
        SubmittedMotherQualification = snapshot.MotherQualification;
        SubmittedMotherMaskedIdentity = snapshot.MotherMaskedIdentity;
        Touch();
    }

    public void Cancel(string? reason)
    {
        if (Status is not (AdmissionApplicationStatus.Draft or AdmissionApplicationStatus.Submitted))
        {
            throw new InvalidOperationException("Cancellation is not allowed for the current status.");
        }

        if (Status == AdmissionApplicationStatus.Submitted && ReviewStartedAtUtc is not null)
        {
            throw new InvalidOperationException("Cancellation is not allowed after review has started.");
        }

        Status = AdmissionApplicationStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        CancellationReason = NormalizeOptional(reason);
        Touch();
    }

    /// <summary>School review: Submitted → UnderReview.</summary>
    public void StartReview(string? schoolNotes = null)
    {
        if (Status != AdmissionApplicationStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted applications can enter review.");
        }

        Status = AdmissionApplicationStatus.UnderReview;
        ReviewStartedAtUtc = DateTimeOffset.UtcNow;
        ApplyOptionalSchoolNotes(schoolNotes);
        Touch();
    }

    /// <summary>School review: eligible statuses → Accepted (policy validated by caller).</summary>
    public void Accept(string? schoolNotes = null)
    {
        Status = AdmissionApplicationStatus.Accepted;
        AcceptedAtUtc = DateTimeOffset.UtcNow;
        ClearWaitingListFields();
        ApplyOptionalSchoolNotes(schoolNotes);
        Touch();
    }

    /// <summary>School review: eligible statuses → Rejected (policy validated by caller).</summary>
    public void Reject(string? rejectionReason, string? schoolNotes)
    {
        Status = AdmissionApplicationStatus.Rejected;
        RejectedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = NormalizeOptional(rejectionReason);
        ClearWaitingListFields();
        ApplyOptionalSchoolNotes(schoolNotes);
        Touch();
    }

    public void MoveToMissingDocuments()
    {
        Status = AdmissionApplicationStatus.MissingDocuments;
        Touch();
    }

    public void ResubmitMissingDocuments()
    {
        if (Status != AdmissionApplicationStatus.MissingDocuments)
        {
            throw new InvalidOperationException("Only missing-documents applications can be resubmitted.");
        }

        Status = AdmissionApplicationStatus.UnderReview;
        Touch();
    }

    public void MoveToInterviewRequired()
    {
        Status = AdmissionApplicationStatus.InterviewRequired;
        Touch();
    }

    public void MoveToAssessmentRequired()
    {
        Status = AdmissionApplicationStatus.AssessmentRequired;
        Touch();
    }

    public void MoveToWaitingList(string? parentVisibleReason, int? position, DateOnly? reviewDate)
    {
        Status = AdmissionApplicationStatus.WaitingList;
        WaitingListEnteredAtUtc = DateTimeOffset.UtcNow;
        WaitingListReason = NormalizeOptional(parentVisibleReason);
        WaitingListPosition = position is > 0 ? position : null;
        WaitingListReviewDate = reviewDate;
        Touch();
    }

    public void ReturnToUnderReview()
    {
        Status = AdmissionApplicationStatus.UnderReview;
        ClearWaitingListFields();
        Touch();
    }

    /// <summary>Accepted → Registered. Does not imply payment or contract.</summary>
    public void MarkRegistered()
    {
        if (Status != AdmissionApplicationStatus.Accepted)
        {
            throw new InvalidOperationException("Only accepted applications can be registered.");
        }

        if (RegisteredAtUtc is not null)
        {
            return;
        }

        Status = AdmissionApplicationStatus.Registered;
        RegisteredAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void AddMissingItemsRequest(AdmissionMissingItemsRequest request)
    {
        MissingItemsRequests ??= new List<AdmissionMissingItemsRequest>();
        MissingItemsRequests.Add(request);
        Touch();
    }

    public void AddInterviewAppointment(AdmissionInterviewAppointment appointment)
    {
        InterviewAppointments ??= new List<AdmissionInterviewAppointment>();
        InterviewAppointments.Add(appointment);
        Touch();
    }

    public void AddAssessmentAppointment(AdmissionAssessmentAppointment appointment)
    {
        AssessmentAppointments ??= new List<AdmissionAssessmentAppointment>();
        AssessmentAppointments.Add(appointment);
        Touch();
    }

    public bool HasOperationalInterviewAssessmentRecords() =>
        (InterviewAppointments?.Count ?? 0) > 0 ||
        (AssessmentAppointments?.Count ?? 0) > 0;

    public void AddPolicySnapshot(AdmissionApplicationInterviewAssessmentPolicySnapshot snapshot)
    {
        if (PolicySnapshot is not null)
        {
            throw new InvalidOperationException("A policy snapshot already exists for this application.");
        }

        PolicySnapshot = snapshot;
        Touch();
    }

    public void ReplacePolicySnapshot(AdmissionApplicationInterviewAssessmentPolicySnapshot snapshot)
    {
        PolicySnapshot = snapshot;
        Touch();
    }

    public void ClearPolicySnapshot()
    {
        PolicySnapshot = null;
        Touch();
    }

    public void AddAgeEligibilitySnapshot(AdmissionApplicationChildAgeEligibilitySnapshot snapshot)
    {
        if (AgeEligibilitySnapshot is not null)
        {
            throw new InvalidOperationException("An age eligibility snapshot already exists for this application.");
        }

        AgeEligibilitySnapshot = snapshot;
        Touch();
    }

    public void ReplaceAgeEligibilitySnapshot(AdmissionApplicationChildAgeEligibilitySnapshot snapshot)
    {
        AgeEligibilitySnapshot = snapshot;
        Touch();
    }

    public void ClearAgeEligibilitySnapshot()
    {
        AgeEligibilitySnapshot = null;
        Touch();
    }

    private void ClearWaitingListFields()
    {
        WaitingListReason = null;
        WaitingListPosition = null;
        WaitingListReviewDate = null;
    }

    private void ApplyOptionalSchoolNotes(string? schoolNotes)
    {
        if (schoolNotes is null)
        {
            return;
        }

        SchoolNotes = NormalizeOptional(schoolNotes);
    }

    public void AddHistory(AdmissionApplicationHistory entry)
    {
        History.Add(entry);
    }

    public void AddAttachment(AdmissionApplicationAttachment attachment)
    {
        EnsureDraftOrMissingDocuments();
        Attachments.Add(attachment);
        Touch();
    }

    public void RemoveAttachment(AdmissionApplicationAttachment attachment)
    {
        EnsureDraftOrMissingDocuments();
        Attachments.Remove(attachment);
        Touch();
    }

    public void AddRequirementSnapshot(AdmissionApplicationRequirementSnapshot snapshot)
    {
        RequirementSnapshots ??= new List<AdmissionApplicationRequirementSnapshot>();
        RequirementSnapshots.Add(snapshot);
        Touch();
    }

    /// <summary>
    /// Ensures the snapshots collection exists for backfill when the entity was loaded without Includes.
    /// Only safe when no snapshots exist yet in the database.
    /// </summary>
    public void EnsureRequirementSnapshotsCollection()
    {
        RequirementSnapshots ??= new List<AdmissionApplicationRequirementSnapshot>();
    }

    public void AddQuestionSnapshot(AdmissionApplicationQuestionSnapshot snapshot)
    {
        QuestionSnapshots ??= new List<AdmissionApplicationQuestionSnapshot>();
        QuestionSnapshots.Add(snapshot);
        Touch();
    }

    public void EnsureQuestionSnapshotsCollection()
    {
        QuestionSnapshots ??= new List<AdmissionApplicationQuestionSnapshot>();
        Answers ??= new List<AdmissionApplicationAnswer>();
    }

    /// <summary>
    /// Clears draft question snapshots and answers so a new snapshot set can be created after scope change.
    /// Caller must remove orphaned attachments separately when needed.
    /// </summary>
    public void ClearQuestionSnapshotsAndAnswers()
    {
        EnsureDraft();
        EnsureQuestionSnapshotsCollection();
        Answers.Clear();
        QuestionSnapshots.Clear();
        Touch();
    }

    public void UpsertAnswer(AdmissionApplicationAnswer answer)
    {
        EnsureDraftOrMissingDocuments();
        EnsureQuestionSnapshotsCollection();
        var existing = Answers.FirstOrDefault(item => item.QuestionSnapshotId == answer.QuestionSnapshotId);
        if (existing is not null)
        {
            Answers.Remove(existing);
        }

        Answers.Add(answer);
        Touch();
    }

    public void RemoveAnswer(AdmissionApplicationAnswer answer)
    {
        EnsureDraftOrMissingDocuments();
        Answers.Remove(answer);
        Touch();
    }

    /// <summary>Applies Parent corrections to submitted snapshot fields during MissingDocuments only.</summary>
    public void ApplyMissingChildSnapshotCorrection(
        AdmissionChildSnapshotFieldCode field,
        string? textValue,
        bool? boolValue)
    {
        if (Status != AdmissionApplicationStatus.MissingDocuments)
        {
            throw new InvalidOperationException("Snapshot corrections are only allowed during MissingDocuments.");
        }

        switch (field)
        {
            case AdmissionChildSnapshotFieldCode.FullName:
                SubmittedChildFullName = RequireText(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.CurrentSchoolName:
                SubmittedChildCurrentSchoolName = NormalizeOptional(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.PreferredStudyLanguage:
                SubmittedChildPreferredStudyLanguage = int.TryParse(textValue, out var lang) ? lang : null;
                break;
            case AdmissionChildSnapshotFieldCode.Skills:
                SubmittedChildSkills = NormalizeOptional(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.Hobbies:
                SubmittedChildHobbies = NormalizeOptional(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.Strengths:
                SubmittedChildStrengths = NormalizeOptional(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.ImprovementAreas:
                SubmittedChildImprovementAreas = NormalizeOptional(textValue);
                break;
            case AdmissionChildSnapshotFieldCode.HasSpecialNeeds:
                SubmittedChildHasSpecialNeeds = boolValue;
                break;
            case AdmissionChildSnapshotFieldCode.SpecialNeedsNotes:
                SubmittedChildSpecialNeedsNotes = NormalizeOptional(textValue);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }

        Touch();
    }

    public void ApplyMissingParentSnapshotCorrection(
        AdmissionParentSnapshotFieldCode field,
        string? textValue)
    {
        if (Status != AdmissionApplicationStatus.MissingDocuments)
        {
            throw new InvalidOperationException("Snapshot corrections are only allowed during MissingDocuments.");
        }

        switch (field)
        {
            case AdmissionParentSnapshotFieldCode.DisplayName:
                SubmittedParentDisplayName = RequireText(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.Phone:
                SubmittedParentPhone = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.AlternatePhone:
                SubmittedParentAlternatePhone = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.Email:
                SubmittedParentEmail = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.FatherFullName:
                SubmittedFatherFullName = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.FatherPhone:
                SubmittedFatherPhone = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.FatherEmail:
                SubmittedFatherEmail = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.FatherOccupation:
                SubmittedFatherOccupation = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.FatherQualification:
                SubmittedFatherQualification = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.MotherFullName:
                SubmittedMotherFullName = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.MotherPhone:
                SubmittedMotherPhone = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.MotherEmail:
                SubmittedMotherEmail = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.MotherOccupation:
                SubmittedMotherOccupation = NormalizeOptional(textValue);
                break;
            case AdmissionParentSnapshotFieldCode.MotherQualification:
                SubmittedMotherQualification = NormalizeOptional(textValue);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }

        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != AdmissionApplicationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft applications are editable.");
        }
    }

    private void EnsureDraftOrMissingDocuments()
    {
        if (Status is not (AdmissionApplicationStatus.Draft or AdmissionApplicationStatus.MissingDocuments))
        {
            throw new InvalidOperationException("Only draft or missing-documents applications allow this change.");
        }
    }

    private static string RequireText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.")
            : value.Trim();

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Bounded localized names, approved child fields, and parent/guardian fields at submission.</summary>
public sealed record AdmissionSubmissionSnapshot(
    string ChildFullName,
    string? ChildCurrentSchoolName,
    int? ChildPreferredStudyLanguage,
    string? ChildSkills,
    string? ChildHobbies,
    string? ChildStrengths,
    string? ChildImprovementAreas,
    bool ChildHasSpecialNeeds,
    string? ChildSpecialNeedsNotes,
    string SchoolNameAr,
    string? SchoolNameEn,
    string BranchNameAr,
    string? BranchNameEn,
    string StageNameAr,
    string? StageNameEn,
    string GradeNameAr,
    string? GradeNameEn,
    string AcademicYearNameAr,
    string? AcademicYearNameEn,
    string ParentDisplayName,
    string? ParentEmail,
    string? ParentPhone,
    string? ParentAlternatePhone,
    string? FatherFullName,
    string? FatherPhone,
    string? FatherEmail,
    string? FatherOccupation,
    string? FatherQualification,
    string? FatherMaskedIdentity,
    string? MotherFullName,
    string? MotherPhone,
    string? MotherEmail,
    string? MotherOccupation,
    string? MotherQualification,
    string? MotherMaskedIdentity);
