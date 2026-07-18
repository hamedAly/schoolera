using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Queries.ListSupportTickets;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Queries.ListAdminSupportTickets;

public sealed record ListAdminSupportTicketsQuery(
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
    public static ListAdminSupportTicketsQuery FromFilters(
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

public sealed class ListAdminSupportTicketsQueryValidator : AbstractValidator<ListAdminSupportTicketsQuery>
{
    public ListAdminSupportTicketsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(200).When(query => !string.IsNullOrWhiteSpace(query.Search));
    }
}

public sealed class ListAdminSupportTicketsQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<ListAdminSupportTicketsQueryHandler> logger)
    : IRequestHandler<ListAdminSupportTicketsQuery, Result<PagedResult<SupportTicketListItemDto>>>
{
    public async Task<Result<PagedResult<SupportTicketListItemDto>>> Handle(
        ListAdminSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsPlatformAdmin(currentUser))
        {
            return SupportTicketResults.Forbidden<PagedResult<SupportTicketListItemDto>>();
        }

        var supportQuery = new ListSupportTicketsQuery(
            request.Search,
            request.Status,
            request.Priority,
            request.Category,
            request.AssignedSupportAgentUserId,
            request.UnassignedOnly,
            request.FirstResponseOverdueOnly,
            request.ResolutionOverdueOnly,
            request.PageNumber,
            request.PageSize);

        var filter = BuildFilter(supportQuery);
        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var (items, totalCount) = await ticketRepository.ListForSupportAsync(filter, paging, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var mapped = items.Select(ticket => SupportTicketMapping.ToListItem(ticket, utcNow)).ToArray();

        logger.LogInformation(
            "Listed {Count} admin support tickets (page {PageNumber}).",
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
