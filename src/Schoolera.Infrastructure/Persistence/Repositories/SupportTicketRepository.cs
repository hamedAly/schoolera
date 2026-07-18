using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketRepository(SchooleraDbContext dbContext) : ISupportTicketRepository
{
    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default) =>
        await dbContext.SupportTickets.AddAsync(ticket, cancellationToken);

    public Task<SupportTicket?> GetByIdAsync(
        Guid ticketId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default) =>
        BuildQuery(includeDetails)
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public Task<SupportTicket?> GetOwnedAsync(
        Guid parentUserId,
        Guid ticketId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default) =>
        BuildQuery(includeDetails)
            .FirstOrDefaultAsync(
                ticket => ticket.Id == ticketId && ticket.ParentUserId == parentUserId,
                cancellationToken);

    public Task<SupportTicket?> GetBySourceContactRequestIdAsync(
        Guid contactRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.SupportTickets
            .FirstOrDefaultAsync(
                ticket => ticket.SourceContactRequestId == contactRequestId,
                cancellationToken);

    public Task<SupportTicketAttachment?> GetAttachmentAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<SupportTicketAttachment>()
            .FirstOrDefaultAsync(
                attachment => attachment.Id == attachmentId && attachment.TicketId == ticketId,
                cancellationToken);

    public async Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> ListForParentAsync(
        Guid parentUserId,
        SupportTicketListFilter filter,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(
            dbContext.SupportTickets.AsNoTracking().Where(ticket => ticket.ParentUserId == parentUserId),
            filter);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(ticket => ticket.UpdatedAtUtc)
            .ThenByDescending(ticket => ticket.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> ListForSupportAsync(
        SupportTicketListFilter filter,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(dbContext.SupportTickets.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<SupportTicket>> ListForExportAsync(
        SupportTicketListFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(dbContext.SupportTickets.AsNoTracking(), filter);
        return await query
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Take(maxRows)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<SupportTicket> BuildQuery(bool includeDetails)
    {
        IQueryable<SupportTicket> query = dbContext.SupportTickets;
        if (!includeDetails)
        {
            return query;
        }

        return query
            .Include(ticket => ticket.Messages)
            .Include(ticket => ticket.History)
            .Include(ticket => ticket.Attachments);
    }

    private static IQueryable<SupportTicket> ApplyFilter(
        IQueryable<SupportTicket> query,
        SupportTicketListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(ticket =>
                ticket.Reference.Contains(term) ||
                ticket.Subject.Contains(term));
        }

        if (filter.Status is { } status)
        {
            query = query.Where(ticket => ticket.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(ticket => ticket.Priority == priority);
        }

        if (filter.Category is { } category)
        {
            query = query.Where(ticket => ticket.Category == category);
        }

        if (filter.AssignedSupportAgentUserId is { } agentId)
        {
            query = query.Where(ticket => ticket.AssignedSupportAgentUserId == agentId);
        }

        if (filter.UnassignedOnly is true)
        {
            query = query.Where(ticket => ticket.AssignedSupportAgentUserId == null);
        }

        if (filter.ParentUserId is { } parentUserId)
        {
            query = query.Where(ticket => ticket.ParentUserId == parentUserId);
        }

        if (filter.AdmissionApplicationId is { } admissionId)
        {
            query = query.Where(ticket => ticket.AdmissionApplicationId == admissionId);
        }

        if (filter.CreatedFromUtc is { } from)
        {
            query = query.Where(ticket => ticket.CreatedAtUtc >= from);
        }

        if (filter.CreatedToUtc is { } to)
        {
            query = query.Where(ticket => ticket.CreatedAtUtc <= to);
        }

        var utcNow = DateTimeOffset.UtcNow;
        if (filter.FirstResponseOverdueOnly is true)
        {
            query = query.Where(ticket =>
                ticket.FirstResponseAtUtc == null &&
                ticket.FirstResponseDueAtUtc < utcNow);
        }

        if (filter.ResolutionOverdueOnly is true)
        {
            query = query.Where(ticket =>
                ticket.Status != SupportTicketStatus.Resolved &&
                ticket.Status != SupportTicketStatus.Closed &&
                ticket.ResolutionDueAtUtc < utcNow);
        }

        return query;
    }
}
