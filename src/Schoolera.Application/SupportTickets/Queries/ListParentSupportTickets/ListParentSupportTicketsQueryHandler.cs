using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Queries.ListParentSupportTickets;

public sealed record ListParentSupportTicketsQuery(
    int? Status = null,
    int? Priority = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<SupportTicketListItemDto>>>
{
    public static ListParentSupportTicketsQuery FromFilters(
        int? status,
        int? priority,
        int pageNumber,
        int pageSize) =>
        new(status, priority, pageNumber, pageSize);
}

public sealed class ListParentSupportTicketsQueryValidator : AbstractValidator<ListParentSupportTicketsQuery>
{
    public ListParentSupportTicketsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
    }
}

public sealed class ListParentSupportTicketsQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<ListParentSupportTicketsQueryHandler> logger)
    : IRequestHandler<ListParentSupportTicketsQuery, Result<PagedResult<SupportTicketListItemDto>>>
{
    public async Task<Result<PagedResult<SupportTicketListItemDto>>> Handle(
        ListParentSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<PagedResult<SupportTicketListItemDto>>();
        }

        SupportTicketStatus? status = request.Status is { } statusValue &&
                                      Enum.IsDefined(typeof(SupportTicketStatus), statusValue)
            ? (SupportTicketStatus)statusValue
            : null;
        SupportTicketPriority? priority = request.Priority is { } priorityValue &&
                                          Enum.IsDefined(typeof(SupportTicketPriority), priorityValue)
            ? (SupportTicketPriority)priorityValue
            : null;

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var filter = new SupportTicketListFilter(Status: status, Priority: priority);
        var (items, totalCount) = await ticketRepository.ListForParentAsync(
            userId,
            filter,
            paging,
            cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var mapped = items.Select(ticket => SupportTicketMapping.ToListItem(ticket, utcNow)).ToArray();

        logger.LogInformation(
            "Listed {Count} parent support tickets for user {UserId} (page {PageNumber}).",
            mapped.Length,
            userId,
            paging.NormalizedPageNumber);

        return Result<PagedResult<SupportTicketListItemDto>>.Success(
            PagedResult<SupportTicketListItemDto>.Create(mapped, totalCount, paging));
    }
}
