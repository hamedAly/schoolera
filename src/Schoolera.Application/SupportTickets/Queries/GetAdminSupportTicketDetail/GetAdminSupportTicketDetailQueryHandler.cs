using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;

namespace Schoolera.Application.SupportTickets.Queries.GetAdminSupportTicketDetail;

public sealed record GetAdminSupportTicketDetailQuery(Guid TicketId)
    : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class GetAdminSupportTicketDetailQueryValidator : AbstractValidator<GetAdminSupportTicketDetailQuery>
{
    public GetAdminSupportTicketDetailQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

public sealed class GetAdminSupportTicketDetailQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IUserDirectory userDirectory,
    ILogger<GetAdminSupportTicketDetailQueryHandler> logger)
    : IRequestHandler<GetAdminSupportTicketDetailQuery, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        GetAdminSupportTicketDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsPlatformAdmin(currentUser))
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

        var utcNow = DateTimeOffset.UtcNow;
        var parents = await userDirectory.GetUsersAsync([ticket.ParentUserId], cancellationToken);
        parents.TryGetValue(ticket.ParentUserId, out var parent);

        logger.LogInformation("Platform admin loaded support ticket {TicketId}.", ticket.Id);

        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(ticket, parent, utcNow));
    }
}
