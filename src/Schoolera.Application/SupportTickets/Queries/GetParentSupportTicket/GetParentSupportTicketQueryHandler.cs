using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;

namespace Schoolera.Application.SupportTickets.Queries.GetParentSupportTicket;

public sealed record GetParentSupportTicketQuery(Guid TicketId)
    : IRequest<Result<SupportTicketParentDetailDto>>;

public sealed class GetParentSupportTicketQueryValidator : AbstractValidator<GetParentSupportTicketQuery>
{
    public GetParentSupportTicketQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

public sealed class GetParentSupportTicketQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<GetParentSupportTicketQueryHandler> logger)
    : IRequestHandler<GetParentSupportTicketQuery, Result<SupportTicketParentDetailDto>>
{
    public async Task<Result<SupportTicketParentDetailDto>> Handle(
        GetParentSupportTicketQuery request,
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

        logger.LogInformation("Parent {UserId} loaded support ticket {TicketId}.", userId, ticket.Id);

        return Result<SupportTicketParentDetailDto>.Success(SupportTicketMapping.ToParentDetail(ticket));
    }
}
