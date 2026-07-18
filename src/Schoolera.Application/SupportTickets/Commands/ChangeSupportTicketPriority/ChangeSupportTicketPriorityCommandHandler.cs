using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketPriority;

public sealed record ChangeSupportTicketPriorityCommand(
    Guid TicketId,
    SupportTicketPriorityChangeRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class ChangeSupportTicketPriorityCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ILogger<ChangeSupportTicketPriorityCommandHandler> logger)
    : IRequestHandler<ChangeSupportTicketPriorityCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        ChangeSupportTicketPriorityCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        if (!Enum.IsDefined(typeof(SupportTicketPriority), request.Body.Priority))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Invalid priority.",
                SupportTicketErrorCodes.InvalidPriority);
        }

        var ticket = await ticketRepository.GetByIdAsync(
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketSupportDetailDto>();
        }

        var previous = ticket.Priority;
        var next = (SupportTicketPriority)request.Body.Priority;
        if (previous == next)
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Invalid priority transition.",
                SupportTicketErrorCodes.InvalidPriority);
        }

        ticket.SetPriority(next);
        ticket.AddHistory(
            SupportTicketHistoryAction.PriorityChanged,
            userId,
            fromValue: previous.ToString(),
            toValue: next.ToString(),
            summary: null);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "User {UserId} changed support ticket {TicketId} priority from {From} to {To}.",
            userId,
            ticket.Id,
            previous,
            next);

        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
