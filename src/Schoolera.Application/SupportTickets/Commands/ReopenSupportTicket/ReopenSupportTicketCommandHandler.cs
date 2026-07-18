using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ReopenSupportTicket;

public sealed record ReopenSupportTicketCommand(Guid TicketId)
    : IRequest<Result<SupportTicketParentDetailDto>>;

public sealed class ReopenSupportTicketCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IOptions<SupportTicketSlaOptions> slaOptions,
    IUnitOfWork unitOfWork,
    ILogger<ReopenSupportTicketCommandHandler> logger)
    : IRequestHandler<ReopenSupportTicketCommand, Result<SupportTicketParentDetailDto>>
{
    public async Task<Result<SupportTicketParentDetailDto>> Handle(
        ReopenSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketParentDetailDto>();
        }

        var ticket = await ticketRepository.GetOwnedAsync(
            userId,
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketParentDetailDto>();
        }

        var utcNow = DateTimeOffset.UtcNow;
        if (!SupportTicketTransitionPolicy.CanParentReopen(
                ticket.Status,
                ticket.ResolvedAtUtc,
                utcNow,
                slaOptions.Value.ReopenWindowHours))
        {
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "The reopen window has expired or the ticket cannot be reopened.",
                SupportTicketErrorCodes.ReopenWindowExpired);
        }

        var previous = ticket.Status;
        ticket.ChangeStatus(SupportTicketStatus.Open, utcNow);
        ticket.AddHistory(
            SupportTicketHistoryAction.Reopened,
            userId,
            fromValue: previous.ToString(),
            toValue: ticket.Status.ToString(),
            summary: null);

        await SupportTicketNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            ticket,
            NotificationEventType.SupportTicketReopened,
            "reopened",
            cancellationToken);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketParentDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Parent {UserId} reopened support ticket {TicketId}.", userId, ticket.Id);

        var reloaded = await ticketRepository.GetOwnedAsync(
            userId,
            ticket.Id,
            includeDetails: true,
            cancellationToken) ?? ticket;
        return Result<SupportTicketParentDetailDto>.Success(SupportTicketMapping.ToParentDetail(reloaded));
    }
}
