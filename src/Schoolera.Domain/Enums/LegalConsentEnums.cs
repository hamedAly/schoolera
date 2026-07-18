namespace Schoolera.Domain.Enums;

/// <summary>Versioned legal document kinds bound to CMS terms/privacy.</summary>
public enum LegalDocumentType
{
    Terms = 1,
    Privacy = 2,
}

/// <summary>Why a user accepted a legal document version.</summary>
public enum LegalAcceptancePurpose
{
    ParentRegistration = 1,
    AdmissionSubmission = 2,
    PaymentInitiation = 3,
    FinancingInitiation = 4,
}
