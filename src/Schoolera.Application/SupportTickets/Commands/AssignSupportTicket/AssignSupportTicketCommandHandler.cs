using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.AssignSupportTicket;

public sealed record AssignSupportTicketCommand(
    Guid TicketId,
    SupportTicketAssignRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class AssignSupportTicketCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ILogger<AssignSupportTicketCommandHandler> logger)
    : IRequestHandler<AssignSupportTicketCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        AssignSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsPlatformAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        var agentId = request.Body.SupportAgentUserId;
        var users = await userDirectory.GetUsersAsync([agentId], cancellationToken);
        if (!users.ContainsKey(agentId))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Support agent not found.",
                SupportTicketErrorCodes.AgentNotFound);
        }

        var roles = await userDirectory.GetRolesAsync(agentId, cancellationToken);
        if (!roles.Contains(SchooleraRoles.SupportAgent) &&
            !roles.Contains(SchooleraRoles.PlatformAdmin))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Support agent not found.",
                SupportTicketErrorCodes.AgentNotFound);
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
        ticket.Assign(agentId);
        ticket.AddHistory(
            SupportTicketHistoryAction.Assigned,
            userId,
            fromValue: previous?.ToString("D"),
            toValue: agentId.ToString("D"),
            summary: null);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Platform admin {UserId} assigned support ticket {TicketId} to agent {AgentId}.",
            userId,
            ticket.Id,
            agentId);

        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
