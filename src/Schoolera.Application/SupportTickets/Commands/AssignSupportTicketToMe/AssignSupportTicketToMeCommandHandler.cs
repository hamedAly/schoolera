using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.AssignSupportTicketToMe;

public sealed record AssignSupportTicketToMeCommand(Guid TicketId)
    : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class AssignSupportTicketToMeCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ILogger<AssignSupportTicketToMeCommandHandler> logger)
    : IRequestHandler<AssignSupportTicketToMeCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        AssignSupportTicketToMeCommand request,
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

        var previous = ticket.AssignedSupportAgentUserId;
        ticket.Assign(userId);
        ticket.AddHistory(
            SupportTicketHistoryAction.Assigned,
            userId,
            fromValue: previous?.ToString("D"),
            toValue: userId.ToString("D"),
            summary: null);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("User {UserId} assigned support ticket {TicketId} to self.", userId, ticket.Id);

        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
