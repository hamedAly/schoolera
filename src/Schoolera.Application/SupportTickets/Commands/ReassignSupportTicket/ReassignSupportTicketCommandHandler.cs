using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ReassignSupportTicket;

public sealed record ReassignSupportTicketCommand(
    Guid TicketId,
    SupportTicketAssignRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class ReassignSupportTicketCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ILogger<ReassignSupportTicketCommandHandler> logger)
    : IRequestHandler<ReassignSupportTicketCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        ReassignSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        var agentId = request.Body.SupportAgentUserId;
        if (!await IsAssignableAgentAsync(agentId, cancellationToken))
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
            "User {UserId} reassigned support ticket {TicketId} to agent {AgentId}.",
            userId,
            ticket.Id,
            agentId);

        return await ReloadDetailAsync(ticket.Id, cancellationToken);
    }

    private async Task<bool> IsAssignableAgentAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var users = await userDirectory.GetUsersAsync([agentId], cancellationToken);
        if (!users.ContainsKey(agentId))
        {
            return false;
        }

        var roles = await userDirectory.GetRolesAsync(agentId, cancellationToken);
        return roles.Contains(SchooleraRoles.SupportAgent) ||
               roles.Contains(SchooleraRoles.PlatformAdmin);
    }

    private async Task<Result<SupportTicketSupportDetailDto>> ReloadDetailAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticketId, includeDetails: true, cancellationToken);
        if (reloaded is null)
        {
            return SupportTicketResults.NotFound<SupportTicketSupportDetailDto>();
        }

        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
