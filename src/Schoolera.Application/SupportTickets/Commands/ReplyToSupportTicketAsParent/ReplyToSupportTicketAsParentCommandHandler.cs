using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsParent;

public sealed record ReplyToSupportTicketAsParentCommand(
    Guid TicketId,
    SupportTicketReplyRequest Body) : IRequest<Result<SupportTicketParentDetailDto>>;

public sealed class ReplyToSupportTicketAsParentCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReplyToSupportTicketAsParentCommandHandler> logger)
    : IRequestHandler<ReplyToSupportTicketAsParentCommand, Result<SupportTicketParentDetailDto>>
{
    public async Task<Result<SupportTicketParentDetailDto>> Handle(
        ReplyToSupportTicketAsParentCommand request,
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

        if (ticket.Status is SupportTicketStatus.Closed or SupportTicketStatus.Resolved)
        {
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "Replies are not allowed for the current ticket status.",
                SupportTicketErrorCodes.InvalidStatus);
        }

        ticket.AddMessage(
            userId,
            SupportTicketAuthorType.Parent,
            SupportTicketMessageVisibility.CustomerVisible,
            request.Body.Body);

        ticket.AddHistory(
            SupportTicketHistoryAction.MessageAdded,
            userId,
            fromValue: null,
            toValue: nameof(SupportTicketMessageVisibility.CustomerVisible),
            summary: null);

        if (ticket.Status == SupportTicketStatus.WaitingForCustomer)
        {
            var previous = ticket.Status;
            ticket.ChangeStatus(SupportTicketStatus.InProgress, DateTimeOffset.UtcNow);
            ticket.AddHistory(
                SupportTicketHistoryAction.StatusChanged,
                userId,
                fromValue: previous.ToString(),
                toValue: ticket.Status.ToString(),
                summary: null);
        }

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketParentDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Parent {UserId} replied to support ticket {TicketId}.", userId, ticket.Id);

        var reloaded = await ticketRepository.GetOwnedAsync(
            userId,
            ticket.Id,
            includeDetails: true,
            cancellationToken) ?? ticket;
        return Result<SupportTicketParentDetailDto>.Success(SupportTicketMapping.ToParentDetail(reloaded));
    }
}
