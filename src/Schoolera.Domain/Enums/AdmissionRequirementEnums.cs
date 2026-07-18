namespace Schoolera.Domain.Enums;

/// <summary>Server-controlled admission requirement kinds. Client cannot invent kinds.</summary>
public enum AdmissionRequirementKind
{
    InformationalText = 1,
    ParentProfileField = 2,
    ChildProfileField = 3,
    ApplicationDocument = 4,
}

/// <summary>Draft definitions are editable; Published definitions apply to new drafts.</summary>
public enum AdmissionRequirementPublicationStatus
{
    Draft = 1,
    Published = 2,
}

/// <summary>
/// Server-controlled allowlisted Parent/Child profile field codes schools may require.
/// Excludes health notes, identity ciphertext/HMAC, consent, and passwords.
/// </summary>
public enum AdmissionProfileFieldCode
{
    ParentOccupation = 1,
    ParentQualification = 2,
    FatherFullName = 10,
    FatherPhone = 11,
    FatherOccupation = 12,
    FatherQualification = 13,
    MotherFullName = 20,
    MotherPhone = 21,
    MotherOccupation = 22,
    MotherQualification = 23,
    ChildCurrentSchoolName = 30,
    ChildPreferredStudyLanguage = 31,
    ChildHasSpecialNeeds = 32,
    ChildSpecialNeedsNotes = 33,
    ChildSkills = 34,
    ChildHobbies = 35,
    ChildStrengths = 36,
    ChildImprovementAreas = 37,
}

/// <summary>Server-controlled document slots for ApplicationDocument requirements.</summary>
public enum AdmissionRequiredDocumentCode
{
    BirthCertificate = 1,
    ChildPhoto = 2,
    PreviousSchoolCertificate = 3,
    MedicalReport = 4,
    SupportingDocument = 5,
    OtherApproved = 99,
}
