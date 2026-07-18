namespace Schoolera.Domain.Enums;

public enum SupportTicketStatus
{
    Open = 1,
    InProgress = 2,
    WaitingForCustomer = 3,
    Resolved = 4,
    Closed = 5,
}

public enum SupportTicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4,
}

public enum SupportTicketCategory
{
    GeneralSupport = 1,
    Account = 2,
    SchoolInformation = 3,
    AdmissionApplication = 4,
    Documents = 5,
    TechnicalIssue = 6,
    Other = 7,
}

public enum SupportTicketMessageVisibility
{
    CustomerVisible = 1,
    InternalSupportNote = 2,
}

public enum SupportTicketAuthorType
{
    Parent = 1,
    SupportAgent = 2,
    PlatformAdmin = 3,
    System = 4,
}

public enum SupportTicketHistoryAction
{
    Created = 1,
    StatusChanged = 2,
    PriorityChanged = 3,
    CategoryChanged = 4,
    Assigned = 5,
    Unassigned = 6,
    MessageAdded = 7,
    Reopened = 8,
    AttachmentAdded = 9,
    ConvertedFromContactRequest = 10,
}
