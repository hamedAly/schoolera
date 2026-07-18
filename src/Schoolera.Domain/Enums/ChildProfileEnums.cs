namespace Schoolera.Domain.Enums;

/// <summary>Gender for an individual child (not school enrollment gender).</summary>
public enum ChildGender
{
    Male = 1,
    Female = 2,
}

/// <summary>Supported child identity document kinds (generic format validation only).</summary>
public enum ChildIdentityType
{
    NationalId = 1,
    ResidencyId = 2,
}

/// <summary>Parent preferred contact channel.</summary>
public enum PreferredContactMethod
{
    Phone = 1,
    WhatsApp = 2,
    Email = 3,
}

/// <summary>
/// Controlled preferred study-language values for a child.
/// Smallest architecture-consistent option (no separate Language taxonomy exists).
/// </summary>
public enum ChildStudyLanguage
{
    Arabic = 1,
    English = 2,
    French = 3,
    German = 4,
    Other = 99,
}

/// <summary>Server-controlled private Child Document Vault types.</summary>
public enum ChildDocumentType
{
    BirthCertificate = 1,
    ChildPhoto = 2,
    PreviousSchoolCertificate = 3,
    MedicalReport = 4,
    OtherApproved = 99,
}
