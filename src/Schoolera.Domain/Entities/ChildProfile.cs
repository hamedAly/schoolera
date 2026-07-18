using System.Text.RegularExpressions;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Child belonging to a Parent. Identity plaintext is never stored; only protected + lookup hash + last four.</summary>
public sealed class ChildProfile
{
    private static readonly Regex Htmlish = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

    private ChildProfile()
    {
    }

    public ChildProfile(
        Guid parentProfileId,
        Guid parentUserId,
        string fullName,
        ChildIdentityType identityType,
        string protectedIdentityValue,
        string identityLookupHash,
        string identityLastFour,
        DateOnly birthDate,
        ChildGender gender,
        Guid currentGradeId,
        bool hasSpecialNeeds,
        string? specialNeedsNotes,
        string? currentSchoolName = null,
        ChildStudyLanguage? preferredStudyLanguage = null,
        string? skills = null,
        string? hobbies = null,
        string? strengths = null,
        string? improvementAreas = null,
        string? healthNotes = null)
    {
        Id = Guid.NewGuid();
        ParentProfileId = parentProfileId;
        ParentUserId = parentUserId;
        ApplyCore(
            fullName,
            identityType,
            protectedIdentityValue,
            identityLookupHash,
            identityLastFour,
            birthDate,
            gender,
            currentGradeId,
            hasSpecialNeeds,
            specialNeedsNotes,
            currentSchoolName,
            preferredStudyLanguage,
            skills,
            hobbies,
            strengths,
            improvementAreas,
            healthNotes);
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ParentProfileId { get; private set; }

    public ParentProfile ParentProfile { get; private set; } = null!;

    /// <summary>Denormalized owner for ownership queries and uniqueness (ParentUserId + IdentityLookupHash).</summary>
    public Guid ParentUserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public ChildIdentityType IdentityType { get; private set; }

    public string ProtectedIdentityValue { get; private set; } = string.Empty;

    public string IdentityLookupHash { get; private set; } = string.Empty;

    public string IdentityLastFour { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; }

    public ChildGender Gender { get; private set; }

    public Guid CurrentGradeId { get; private set; }

    public Grade CurrentGrade { get; private set; } = null!;

    public string? CurrentSchoolName { get; private set; }

    public ChildStudyLanguage? PreferredStudyLanguage { get; private set; }

    public string? Skills { get; private set; }

    public string? Hobbies { get; private set; }

    public string? Strengths { get; private set; }

    public string? ImprovementAreas { get; private set; }

    public bool HasSpecialNeeds { get; private set; }

    public string? SpecialNeedsNotes { get; private set; }

    /// <summary>Sensitive health/medical notes. Parent detail only — never list or school DTOs.</summary>
    public string? HealthNotes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<ChildDocument> Documents { get; private set; } = [];

    public void UpdateWithoutIdentity(
        string fullName,
        DateOnly birthDate,
        ChildGender gender,
        Guid currentGradeId,
        bool hasSpecialNeeds,
        string? specialNeedsNotes,
        string? currentSchoolName,
        ChildStudyLanguage? preferredStudyLanguage,
        string? skills,
        string? hobbies,
        string? strengths,
        string? improvementAreas,
        string? healthNotes)
    {
        FullName = NormalizeRequired(fullName);
        BirthDate = birthDate;
        Gender = gender;
        CurrentGradeId = currentGradeId;
        HasSpecialNeeds = hasSpecialNeeds;
        SpecialNeedsNotes = hasSpecialNeeds
            ? NormalizeOptional(specialNeedsNotes)
            : null;
        CurrentSchoolName = NormalizeOptional(currentSchoolName);
        PreferredStudyLanguage = preferredStudyLanguage;
        Skills = NormalizeOptional(skills);
        Hobbies = NormalizeOptional(hobbies);
        Strengths = NormalizeOptional(strengths);
        ImprovementAreas = NormalizeOptional(improvementAreas);
        HealthNotes = NormalizeOptional(healthNotes);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ReplaceIdentity(
        ChildIdentityType identityType,
        string protectedIdentityValue,
        string identityLookupHash,
        string identityLastFour)
    {
        IdentityType = identityType;
        ProtectedIdentityValue = protectedIdentityValue;
        IdentityLookupHash = identityLookupHash;
        IdentityLastFour = identityLastFour;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void ApplyCore(
        string fullName,
        ChildIdentityType identityType,
        string protectedIdentityValue,
        string identityLookupHash,
        string identityLastFour,
        DateOnly birthDate,
        ChildGender gender,
        Guid currentGradeId,
        bool hasSpecialNeeds,
        string? specialNeedsNotes,
        string? currentSchoolName,
        ChildStudyLanguage? preferredStudyLanguage,
        string? skills,
        string? hobbies,
        string? strengths,
        string? improvementAreas,
        string? healthNotes)
    {
        FullName = NormalizeRequired(fullName);
        IdentityType = identityType;
        ProtectedIdentityValue = protectedIdentityValue;
        IdentityLookupHash = identityLookupHash;
        IdentityLastFour = identityLastFour;
        BirthDate = birthDate;
        Gender = gender;
        CurrentGradeId = currentGradeId;
        HasSpecialNeeds = hasSpecialNeeds;
        SpecialNeedsNotes = hasSpecialNeeds
            ? NormalizeOptional(specialNeedsNotes)
            : null;
        CurrentSchoolName = NormalizeOptional(currentSchoolName);
        PreferredStudyLanguage = preferredStudyLanguage;
        Skills = NormalizeOptional(skills);
        Hobbies = NormalizeOptional(hobbies);
        Strengths = NormalizeOptional(strengths);
        ImprovementAreas = NormalizeOptional(improvementAreas);
        HealthNotes = NormalizeOptional(healthNotes);
    }

    internal static string NormalizeRequired(string value) =>
        NormalizeOptional(value) ?? string.Empty;

    internal static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var withoutTags = Htmlish.Replace(value, string.Empty);
        var collapsed = MultiWhitespace.Replace(withoutTags, " ").Trim();
        return string.IsNullOrWhiteSpace(collapsed) ? null : collapsed;
    }
}
