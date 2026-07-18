using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Queries.ListSupportTickets;

public sealed record ListSupportTicketsQuery(
    string? Search = null,
    int? Status = null,
    int? Priority = null,
    int? Category = null,
    Guid? AssignedSupportAgentUserId = null,
    bool? UnassignedOnly = null,
    bool? FirstResponseOverdueOnly = null,
    bool? ResolutionOverdueOnly = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<SupportTicketListItemDto>>>
{
    public static ListSupportTicketsQuery FromFilters(
        string? search,
        int? status,
        int? priority,
        int? category,
        Guid? assignedSupportAgentUserId,
        bool? unassignedOnly,
        bool? firstResponseOverdueOnly,
        bool? resolutionOverdueOnly,
        int pageNumber,
        int pageSize) =>
        new(
            search,
            status,
            priority,
            category,
            assignedSupportAgentUserId,
            unassignedOnly,
            firstResponseOverdueOnly,
            resolutionOverdueOnly,
            pageNumber,
            pageSize);
}

public sealed class ListSupportTicketsQueryValidator : AbstractValidator<ListSupportTicketsQuery>
{
    public ListSupportTicketsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(200).When(query => !string.IsNullOrWhiteSpace(query.Search));
    }
}

public sealed class ListSupportTicketsQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<ListSupportTicketsQueryHandler> logger)
    : IRequestHandler<ListSupportTicketsQuery, Result<PagedResult<SupportTicketListItemDto>>>
{
    public async Task<Result<PagedResult<SupportTicketListItemDto>>> Handle(
        ListSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser))
        {
            return SupportTicketResults.Forbidden<PagedResult<SupportTicketListItemDto>>();
        }

        var filter = BuildFilter(request);
        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var (items, totalCount) = await ticketRepository.ListForSupportAsync(filter, paging, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var mapped = items.Select(ticket => SupportTicketMapping.ToListItem(ticket, utcNow)).ToArray();

        logger.LogInformation(
            "Listed {Count} support tickets (page {PageNumber}).",
            mapped.Length,
            paging.NormalizedPageNumber);

        return Result<PagedResult<SupportTicketListItemDto>>.Success(
            PagedResult<SupportTicketListItemDto>.Create(mapped, totalCount, paging));
    }

    private static SupportTicketListFilter BuildFilter(ListSupportTicketsQuery request)
    {
        SupportTicketStatus? status = request.Status is { } statusValue &&
                                      Enum.IsDefined(typeof(SupportTicketStatus), statusValue)
            ? (SupportTicketStatus)statusValue
            : null;
        SupportTicketPriority? priority = request.Priority is { } priorityValue &&
                                          Enum.IsDefined(typeof(SupportTicketPriority), priorityValue)
            ? (SupportTicketPriority)priorityValue
            : null;
        SupportTicketCategory? category = request.Category is { } categoryValue &&
                                          Enum.IsDefined(typeof(SupportTicketCategory), categoryValue)
            ? (SupportTicketCategory)categoryValue
            : null;

        return new SupportTicketListFilter(
            Search: request.Search,
            Status: status,
            Priority: priority,
            Category: category,
            AssignedSupportAgentUserId: request.AssignedSupportAgentUserId,
            UnassignedOnly: request.UnassignedOnly,
            FirstResponseOverdueOnly: request.FirstResponseOverdueOnly,
            ResolutionOverdueOnly: request.ResolutionOverdueOnly);
    }
}
