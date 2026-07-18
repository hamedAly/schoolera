using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketStatus;

public sealed record ChangeSupportTicketStatusCommand(
    Guid TicketId,
    SupportTicketStatusChangeRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class ChangeSupportTicketStatusCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IUnitOfWork unitOfWork,
    ILogger<ChangeSupportTicketStatusCommandHandler> logger)
    : IRequestHandler<ChangeSupportTicketStatusCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        ChangeSupportTicketStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        if (!Enum.IsDefined(typeof(SupportTicketStatus), request.Body.Status))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Invalid status.",
                SupportTicketErrorCodes.InvalidStatus);
        }

        var nextStatus = (SupportTicketStatus)request.Body.Status;
        var ticket = await ticketRepository.GetByIdAsync(
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketSupportDetailDto>();
        }

        var previous = ticket.Status;
        if (!SupportTicketTransitionPolicy.CanTransition(previous, nextStatus))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Invalid status transition.",
                SupportTicketErrorCodes.InvalidTransition);
        }

        var utcNow = DateTimeOffset.UtcNow;
        ticket.ChangeStatus(nextStatus, utcNow);
        ticket.AddHistory(
            SupportTicketHistoryAction.StatusChanged,
            userId,
            fromValue: previous.ToString(),
            toValue: nextStatus.ToString(),
            summary: null);

        await EnqueueStatusNotificationAsync(ticket, nextStatus, notificationPublisher, parentAccountService, cancellationToken);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "User {UserId} changed support ticket {TicketId} status from {From} to {To}.",
            userId,
            ticket.Id,
            previous,
            nextStatus);

        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }

    private static async Task EnqueueStatusNotificationAsync(
        SupportTicket ticket,
        SupportTicketStatus status,
        INotificationOutboxPublisher notificationPublisher,
        IParentAccountService parentAccountService,
        CancellationToken cancellationToken)
    {
        var (eventType, actionKey) = status switch
        {
            SupportTicketStatus.WaitingForCustomer =>
                (NotificationEventType.SupportTicketWaitingForCustomer, "waiting"),
            SupportTicketStatus.Resolved =>
                (NotificationEventType.SupportTicketResolved, "resolved"),
            SupportTicketStatus.Closed =>
                (NotificationEventType.SupportTicketClosed, "closed"),
            _ => ((NotificationEventType?)null, (string?)null),
        };

        if (eventType is null)
        {
            return;
        }

        await SupportTicketNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            ticket,
            eventType.Value,
            actionKey!,
            cancellationToken);
    }
}
