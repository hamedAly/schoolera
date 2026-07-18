namespace Schoolera.Application.SupportTickets.Dtos;

public sealed record CreateSupportTicketRequest(
    string Subject,
    string Body,
    int Category,
    int Priority = 2,
    Guid? AdmissionApplicationId = null);

public sealed record SupportTicketReplyRequest(string Body);

public sealed record SupportTicketStatusChangeRequest(int Status);

public sealed record SupportTicketPriorityChangeRequest(int Priority);

public sealed record SupportTicketCategoryChangeRequest(int Category);

public sealed record SupportTicketAssignRequest(Guid SupportAgentUserId);

public sealed record SupportTicketListItemDto(
    Guid Id,
    string Reference,
    string Subject,
    int Status,
    int Priority,
    int Category,
    Guid ParentUserId,
    Guid? AssignedSupportAgentUserId,
    Guid? AdmissionApplicationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset FirstResponseDueAtUtc,
    DateTimeOffset ResolutionDueAtUtc,
    bool IsFirstResponseOverdue,
    bool IsResolutionOverdue);

public sealed record SupportTicketMessageDto(
    Guid Id,
    Guid AuthorUserId,
    int AuthorType,
    int Visibility,
    string Body,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<SupportTicketAttachmentDto> Attachments);

public sealed record SupportTicketAttachmentDto(
    Guid Id,
    Guid? MessageId,
    int Visibility,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record SupportTicketHistoryDto(
    Guid Id,
    int Action,
    Guid? ActorUserId,
    string? FromValue,
    string? ToValue,
    string? Summary,
    DateTimeOffset CreatedAtUtc);

public sealed record SupportTicketParentDetailDto(
    Guid Id,
    string Reference,
    string Subject,
    int Status,
    int Priority,
    int Category,
    Guid? AdmissionApplicationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyList<SupportTicketMessageDto> Messages,
    IReadOnlyList<SupportTicketAttachmentDto> Attachments);

public sealed record SupportTicketSupportDetailDto(
    Guid Id,
    string Reference,
    string Subject,
    int Status,
    int Priority,
    int Category,
    Guid ParentUserId,
    string? ParentDisplayName,
    string? ParentEmail,
    Guid? AssignedSupportAgentUserId,
    Guid? AdmissionApplicationId,
    Guid? SourceContactRequestId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset FirstResponseDueAtUtc,
    DateTimeOffset ResolutionDueAtUtc,
    bool IsFirstResponseOverdue,
    bool IsResolutionOverdue,
    IReadOnlyList<SupportTicketMessageDto> Messages,
    IReadOnlyList<SupportTicketAttachmentDto> Attachments,
    IReadOnlyList<SupportTicketHistoryDto> History);

public sealed record SupportTicketAttachmentDownloadDto(
    Stream Content,
    string ContentType,
    string DownloadFileName,
    long SizeBytes);

public sealed record SupportTicketExportFileDto(byte[] Content, string FileName);
