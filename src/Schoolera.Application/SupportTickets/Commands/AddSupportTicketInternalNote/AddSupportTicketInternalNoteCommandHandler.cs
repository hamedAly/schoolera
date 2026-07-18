using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.AddSupportTicketInternalNote;

public sealed record AddSupportTicketInternalNoteCommand(
    Guid TicketId,
    SupportTicketReplyRequest Body) : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class AddSupportTicketInternalNoteCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    ILogger<AddSupportTicketInternalNoteCommandHandler> logger)
    : IRequestHandler<AddSupportTicketInternalNoteCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        AddSupportTicketInternalNoteCommand request,
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

        var authorType = SupportTicketAccess.ResolveAuthorType(currentUser);
        ticket.AddMessage(
            userId,
            authorType,
            SupportTicketMessageVisibility.InternalSupportNote,
            request.Body.Body);

        ticket.AddHistory(
            SupportTicketHistoryAction.MessageAdded,
            userId,
            fromValue: null,
            toValue: nameof(SupportTicketMessageVisibility.InternalSupportNote),
            summary: null);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Support user {UserId} added internal note to ticket {TicketId}.", userId, ticket.Id);

        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
