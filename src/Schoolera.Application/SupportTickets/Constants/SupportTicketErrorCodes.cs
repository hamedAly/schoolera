namespace Schoolera.Application.SupportTickets.Constants;

public static class SupportTicketErrorCodes
{
    public const string Forbidden = "supportTicket.forbidden";
    public const string NotFound = "supportTicket.notFound";
    public const string InvalidTransition = "supportTicket.invalidTransition";
    public const string ReopenWindowExpired = "supportTicket.reopenWindowExpired";
    public const string InvalidCategory = "supportTicket.invalidCategory";
    public const string InvalidPriority = "supportTicket.invalidPriority";
    public const string InvalidStatus = "supportTicket.invalidStatus";
    public const string AttachmentNotFound = "supportTicket.attachmentNotFound";
    public const string AttachmentInvalid = "supportTicket.attachmentInvalid";
    public const string ContactNotFound = "supportTicket.contactNotFound";
    public const string ContactEmailMismatch = "supportTicket.contactEmailMismatch";
    public const string ConcurrentUpdate = "supportTicket.concurrentUpdate";
    public const string AdmissionNotFound = "supportTicket.admissionNotFound";
    public const string AgentNotFound = "supportTicket.agentNotFound";
}
