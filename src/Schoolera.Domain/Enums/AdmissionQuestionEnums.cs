namespace Schoolera.Domain.Enums;

/// <summary>Server-controlled admission question types. Clients cannot invent types.</summary>
public enum AdmissionQuestionType
{
    ShortText = 1,
    LongText = 2,
    SingleChoice = 3,
    MultipleChoice = 4,
    Date = 5,
    YesNo = 6,
    File = 7,
}

/// <summary>Draft definitions are editable; Published definitions apply to new question snapshots.</summary>
public enum AdmissionQuestionPublicationStatus
{
    Draft = 1,
    Published = 2,
}
