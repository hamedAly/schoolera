using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsSupport;

public sealed record ReplyToSupportTicketAsSupportCommand(
    Guid TicketId,
    SupportTicketReplyRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class ReplyToSupportTicketAsSupportCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IUnitOfWork unitOfWork,
    ILogger<ReplyToSupportTicketAsSupportCommandHandler> logger)
    : IRequestHandler<ReplyToSupportTicketAsSupportCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        ReplyToSupportTicketAsSupportCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        var ticket = await ticketRepository.GetByIdAsync(
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketSupportDetailDto>();
        }

        if (ticket.Status == SupportTicketStatus.Closed)
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Replies are not allowed for the current ticket status.",
                SupportTicketErrorCodes.InvalidStatus);
        }

        var utcNow = DateTimeOffset.UtcNow;
        var authorType = SupportTicketAccess.ResolveAuthorType(currentUser);

        ticket.AddMessage(
            userId,
            authorType,
            SupportTicketMessageVisibility.CustomerVisible,
            request.Body.Body);

        ticket.AddHistory(
            SupportTicketHistoryAction.MessageAdded,
            userId,
            fromValue: null,
            toValue: nameof(SupportTicketMessageVisibility.CustomerVisible),
            summary: null);

        if (ticket.FirstResponseAtUtc is null)
        {
            ticket.RecordFirstResponse(utcNow);
        }

        if (ticket.Status is SupportTicketStatus.Open or SupportTicketStatus.InProgress)
        {
            var previous = ticket.Status;
            ticket.ChangeStatus(SupportTicketStatus.WaitingForCustomer, utcNow);
            ticket.AddHistory(
                SupportTicketHistoryAction.StatusChanged,
                userId,
                fromValue: previous.ToString(),
                toValue: ticket.Status.ToString(),
                summary: null);

            await SupportTicketNotificationSupport.EnqueueAsync(
                notificationPublisher,
                parentAccountService,
                ticket,
                NotificationEventType.SupportTicketWaitingForCustomer,
                "waiting",
                cancellationToken);
        }

        await SupportTicketNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            ticket,
            NotificationEventType.SupportTicketReply,
            "reply",
            cancellationToken);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Support user {UserId} replied to ticket {TicketId}.", userId, ticket.Id);

        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
