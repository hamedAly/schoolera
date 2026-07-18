using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;

namespace Schoolera.Application.SupportTickets.Queries.ListParentSupportTicketHistory;

public sealed record ListParentSupportTicketHistoryQuery(Guid TicketId)
    : IRequest<Result<IReadOnlyList<SupportTicketHistoryDto>>>;

public sealed class ListParentSupportTicketHistoryQueryValidator
    : AbstractValidator<ListParentSupportTicketHistoryQuery>
{
    public ListParentSupportTicketHistoryQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

public sealed class ListParentSupportTicketHistoryQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<ListParentSupportTicketHistoryQueryHandler> logger)
    : IRequestHandler<ListParentSupportTicketHistoryQuery, Result<IReadOnlyList<SupportTicketHistoryDto>>>
{
    public async Task<Result<IReadOnlyList<SupportTicketHistoryDto>>> Handle(
        ListParentSupportTicketHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<IReadOnlyList<SupportTicketHistoryDto>>();
        }

        var ticket = await ticketRepository.GetOwnedAsync(
            userId,
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<IReadOnlyList<SupportTicketHistoryDto>>();
        }

        var history = ticket.History
            .Where(SupportTicketMapping.IsParentVisibleHistory)
            .OrderBy(entry => entry.CreatedAtUtc)
            .Select(SupportTicketMapping.ToHistory)
            .ToArray();

        logger.LogInformation(
            "Parent {UserId} listed history for support ticket {TicketId}.",
            userId,
            ticket.Id);

        return Result<IReadOnlyList<SupportTicketHistoryDto>>.Success(history);
    }
}
