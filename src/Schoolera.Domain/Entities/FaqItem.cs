using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class FaqItem
{
    private FaqItem()
    {
    }

    /// <summary>General CMS FAQ (platform-owned, no interview category).</summary>
    public FaqItem(
        Guid faqCategoryId,
        string questionAr,
        string questionEn,
        string answerAr,
        string answerEn,
        int sortOrder)
        : this(
            faqCategoryId,
            questionAr,
            questionEn,
            answerAr,
            answerEn,
            sortOrder,
            FaqOwnershipScope.Platform,
            schoolId: null,
            interviewCategory: null,
            schoolBranchId: null,
            educationalStageId: null,
            gradeId: null,
            academicYearId: null)
    {
    }

    /// <summary>Interview/Assessment or school-scoped FAQ.</summary>
    public FaqItem(
        Guid faqCategoryId,
        string questionAr,
        string questionEn,
        string answerAr,
        string answerEn,
        int sortOrder,
        FaqOwnershipScope ownershipScope,
        Guid? schoolId,
        InterviewFaqCategory? interviewCategory,
        Guid? schoolBranchId = null,
        Guid? educationalStageId = null,
        Guid? gradeId = null,
        Guid? academicYearId = null)
    {
        ValidateOwnership(ownershipScope, schoolId, interviewCategory, schoolBranchId, educationalStageId, gradeId, academicYearId);

        Id = Guid.NewGuid();
        FaqCategoryId = faqCategoryId;
        QuestionAr = questionAr.Trim();
        QuestionEn = questionEn.Trim();
        AnswerAr = answerAr.Trim();
        AnswerEn = answerEn.Trim();
        SortOrder = sortOrder;
        OwnershipScope = ownershipScope;
        SchoolId = schoolId;
        InterviewCategory = interviewCategory;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        IsPublished = false;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid FaqCategoryId { get; private set; }

    public FaqCategory FaqCategory { get; private set; } = null!;

    public string QuestionAr { get; private set; } = string.Empty;

    public string QuestionEn { get; private set; } = string.Empty;

    public string AnswerAr { get; private set; } = string.Empty;

    public string AnswerEn { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    public FaqOwnershipScope OwnershipScope { get; private set; }

    public Guid? SchoolId { get; private set; }

    public School? School { get; private set; }

    public InterviewFaqCategory? InterviewCategory { get; private set; }

    public Guid? SchoolBranchId { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public Guid? GradeId { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public bool IsInterviewFaq => InterviewCategory is not null;

    public bool IsGeneralCmsFaq => InterviewCategory is null && OwnershipScope == FaqOwnershipScope.Platform;

    public void Update(
        Guid faqCategoryId,
        string questionAr,
        string questionEn,
        string answerAr,
        string answerEn)
    {
        FaqCategoryId = faqCategoryId;
        QuestionAr = questionAr.Trim();
        QuestionEn = questionEn.Trim();
        AnswerAr = answerAr.Trim();
        AnswerEn = answerEn.Trim();
        Touch();
    }

    public void Update(
        Guid faqCategoryId,
        string questionAr,
        string questionEn,
        string answerAr,
        string answerEn,
        InterviewFaqCategory? interviewCategory,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId)
    {
        ValidateOwnership(OwnershipScope, SchoolId, interviewCategory, schoolBranchId, educationalStageId, gradeId, academicYearId);

        FaqCategoryId = faqCategoryId;
        QuestionAr = questionAr.Trim();
        QuestionEn = questionEn.Trim();
        AnswerAr = answerAr.Trim();
        AnswerEn = answerEn.Trim();
        InterviewCategory = interviewCategory;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Touch();
    }

    public void SetApplicability(
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId)
    {
        if (OwnershipScope != FaqOwnershipScope.School)
        {
            throw new InvalidOperationException("Applicability can only be set on school-owned FAQ items.");
        }

        ValidateOwnership(OwnershipScope, SchoolId, InterviewCategory, schoolBranchId, educationalStageId, gradeId, academicYearId);
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Touch();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        Touch();
    }

    public void Publish()
    {
        IsPublished = true;
        Touch();
    }

    public void Unpublish()
    {
        IsPublished = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void ValidateOwnership(
        FaqOwnershipScope ownershipScope,
        Guid? schoolId,
        InterviewFaqCategory? interviewCategory,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId)
    {
        if (ownershipScope == FaqOwnershipScope.Platform)
        {
            if (schoolId is not null)
            {
                throw new ArgumentException("Platform FAQ items must not have SchoolId.", nameof(schoolId));
            }

            if (schoolBranchId is not null ||
                educationalStageId is not null ||
                gradeId is not null ||
                academicYearId is not null)
            {
                throw new ArgumentException("Platform FAQ items must not have school applicability IDs.");
            }

            return;
        }

        if (ownershipScope == FaqOwnershipScope.School)
        {
            if (schoolId is null || schoolId == Guid.Empty)
            {
                throw new ArgumentException("School FAQ items require SchoolId.", nameof(schoolId));
            }

            if (interviewCategory is null)
            {
                throw new ArgumentException("School FAQ items require InterviewCategory.", nameof(interviewCategory));
            }
        }
    }
}
