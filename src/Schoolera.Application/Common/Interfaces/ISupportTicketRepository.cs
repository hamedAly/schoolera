using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISupportTicketReferenceGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);

    Task<SupportTicket?> GetByIdAsync(
        Guid ticketId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<SupportTicket?> GetOwnedAsync(
        Guid parentUserId,
        Guid ticketId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<SupportTicket?> GetBySourceContactRequestIdAsync(
        Guid contactRequestId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketAttachment?> GetAttachmentAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> ListForParentAsync(
        Guid parentUserId,
        SupportTicketListFilter filter,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> ListForSupportAsync(
        SupportTicketListFilter filter,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportTicket>> ListForExportAsync(
        SupportTicketListFilter filter,
        int maxRows,
        CancellationToken cancellationToken = default);
}

public sealed record SupportTicketListFilter(
    string? Search = null,
    SupportTicketStatus? Status = null,
    SupportTicketPriority? Priority = null,
    SupportTicketCategory? Category = null,
    Guid? AssignedSupportAgentUserId = null,
    bool? UnassignedOnly = null,
    Guid? ParentUserId = null,
    Guid? AdmissionApplicationId = null,
    DateTimeOffset? CreatedFromUtc = null,
    DateTimeOffset? CreatedToUtc = null,
    bool? FirstResponseOverdueOnly = null,
    bool? ResolutionOverdueOnly = null);
