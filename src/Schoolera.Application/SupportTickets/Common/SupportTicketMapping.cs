using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Common;

public static class SupportTicketMapping
{
    public static SupportTicketListItemDto ToListItem(SupportTicket ticket, DateTimeOffset utcNow) =>
        new(
            ticket.Id,
            ticket.Reference,
            ticket.Subject,
            (int)ticket.Status,
            (int)ticket.Priority,
            (int)ticket.Category,
            ticket.ParentUserId,
            ticket.AssignedSupportAgentUserId,
            ticket.AdmissionApplicationId,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.FirstResponseAtUtc,
            ticket.ResolvedAtUtc,
            ticket.FirstResponseDueAtUtc,
            ticket.ResolutionDueAtUtc,
            SupportTicketTransitionPolicy.IsFirstResponseOverdue(
                ticket.FirstResponseAtUtc,
                ticket.FirstResponseDueAtUtc,
                utcNow),
            SupportTicketTransitionPolicy.IsResolutionOverdue(
                ticket.Status,
                ticket.ResolutionDueAtUtc,
                utcNow));

    public static SupportTicketParentDetailDto ToParentDetail(SupportTicket ticket)
    {
        var messages = ticket.Messages
            .Where(message => message.Visibility == SupportTicketMessageVisibility.CustomerVisible)
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => ToMessage(
                message,
                ticket.Attachments.Where(a => a.MessageId == message.Id &&
                    a.Visibility == SupportTicketMessageVisibility.CustomerVisible)))
            .ToArray();

        var attachments = ticket.Attachments
            .Where(a => a.Visibility == SupportTicketMessageVisibility.CustomerVisible)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(ToAttachment)
            .ToArray();

        return new SupportTicketParentDetailDto(
            ticket.Id,
            ticket.Reference,
            ticket.Subject,
            (int)ticket.Status,
            (int)ticket.Priority,
            (int)ticket.Category,
            ticket.AdmissionApplicationId,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.FirstResponseAtUtc,
            ticket.ResolvedAtUtc,
            ticket.ClosedAtUtc,
            messages,
            attachments);
    }

    public static SupportTicketSupportDetailDto ToSupportDetail(
        SupportTicket ticket,
        UserSummary? parent,
        DateTimeOffset utcNow)
    {
        var messages = ticket.Messages
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => ToMessage(
                message,
                ticket.Attachments.Where(a => a.MessageId == message.Id)))
            .ToArray();

        var attachments = ticket.Attachments
            .OrderBy(a => a.CreatedAtUtc)
            .Select(ToAttachment)
            .ToArray();

        var history = ticket.History
            .OrderBy(entry => entry.CreatedAtUtc)
            .Select(ToHistory)
            .ToArray();

        return new SupportTicketSupportDetailDto(
            ticket.Id,
            ticket.Reference,
            ticket.Subject,
            (int)ticket.Status,
            (int)ticket.Priority,
            (int)ticket.Category,
            ticket.ParentUserId,
            parent?.DisplayName,
            parent?.Email,
            ticket.AssignedSupportAgentUserId,
            ticket.AdmissionApplicationId,
            ticket.SourceContactRequestId,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.FirstResponseAtUtc,
            ticket.ResolvedAtUtc,
            ticket.ClosedAtUtc,
            ticket.FirstResponseDueAtUtc,
            ticket.ResolutionDueAtUtc,
            SupportTicketTransitionPolicy.IsFirstResponseOverdue(
                ticket.FirstResponseAtUtc,
                ticket.FirstResponseDueAtUtc,
                utcNow),
            SupportTicketTransitionPolicy.IsResolutionOverdue(
                ticket.Status,
                ticket.ResolutionDueAtUtc,
                utcNow),
            messages,
            attachments,
            history);
    }

    public static SupportTicketHistoryDto ToHistory(SupportTicketHistory entry) =>
        new(
            entry.Id,
            (int)entry.Action,
            entry.ActorUserId,
            entry.FromValue,
            entry.ToValue,
            entry.Summary,
            entry.CreatedAtUtc);

    public static bool IsParentVisibleHistory(SupportTicketHistory entry) =>
        entry.Action is not SupportTicketHistoryAction.MessageAdded ||
        !string.Equals(
            entry.ToValue,
            nameof(SupportTicketMessageVisibility.InternalSupportNote),
            StringComparison.Ordinal);

    private static SupportTicketMessageDto ToMessage(
        SupportTicketMessage message,
        IEnumerable<SupportTicketAttachment> attachments) =>
        new(
            message.Id,
            message.AuthorUserId,
            (int)message.AuthorType,
            (int)message.Visibility,
            message.Body,
            message.CreatedAtUtc,
            attachments.OrderBy(a => a.CreatedAtUtc).Select(ToAttachment).ToArray());

    private static SupportTicketAttachmentDto ToAttachment(SupportTicketAttachment attachment) =>
        new(
            attachment.Id,
            attachment.MessageId,
            (int)attachment.Visibility,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAtUtc);

    public static string SafeOriginalFileName(string originalFileName)
    {
        var name = Path.GetFileName(originalFileName.Trim());
        return string.IsNullOrWhiteSpace(name) ? "attachment.bin" : name;
    }
}
