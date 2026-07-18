using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Queries.ListSupportTickets;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Queries.ExportAdminSupportTickets;

public sealed record ExportAdminSupportTicketsQuery(
    string? Search = null,
    int? Status = null,
    int? Priority = null,
    int? Category = null,
    Guid? AssignedSupportAgentUserId = null,
    bool? UnassignedOnly = null,
    bool? FirstResponseOverdueOnly = null,
    bool? ResolutionOverdueOnly = null) : IRequest<Result<SupportTicketExportFileDto>>
{
    public static ExportAdminSupportTicketsQuery FromFilters(
        string? search,
        int? status,
        int? priority,
        int? category,
        Guid? assignedSupportAgentUserId,
        bool? unassignedOnly,
        bool? firstResponseOverdueOnly,
        bool? resolutionOverdueOnly) =>
        new(
            search,
            status,
            priority,
            category,
            assignedSupportAgentUserId,
            unassignedOnly,
            firstResponseOverdueOnly,
            resolutionOverdueOnly);
}

public sealed class ExportAdminSupportTicketsQueryValidator : AbstractValidator<ExportAdminSupportTicketsQuery>
{
    public ExportAdminSupportTicketsQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(200).When(query => !string.IsNullOrWhiteSpace(query.Search));
    }
}

public sealed class ExportAdminSupportTicketsQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ILogger<ExportAdminSupportTicketsQueryHandler> logger)
    : IRequestHandler<ExportAdminSupportTicketsQuery, Result<SupportTicketExportFileDto>>
{
    public async Task<Result<SupportTicketExportFileDto>> Handle(
        ExportAdminSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsPlatformAdmin(currentUser))
        {
            return SupportTicketResults.Forbidden<SupportTicketExportFileDto>();
        }

        var supportQuery = new ListSupportTicketsQuery(
            request.Search,
            request.Status,
            request.Priority,
            request.Category,
            request.AssignedSupportAgentUserId,
            request.UnassignedOnly,
            request.FirstResponseOverdueOnly,
            request.ResolutionOverdueOnly);

        var filter = BuildFilter(supportQuery);
        var tickets = await ticketRepository.ListForExportAsync(
            filter,
            SupportTicketCsvExporter.MaxExportRows,
            cancellationToken);

        var export = SupportTicketCsvExporter.Build(tickets);

        logger.LogInformation(
            "Exported {Count} admin support tickets to {FileName}.",
            Math.Min(tickets.Count, SupportTicketCsvExporter.MaxExportRows),
            export.FileName);

        return Result<SupportTicketExportFileDto>.Success(export);
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
