using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.SupportTickets.Queries.ExportAdminSupportTickets;

namespace Schoolera.Api.Controllers;

[Route("api/admin/support-tickets")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminSupportTicketsExportController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] int? status,
        [FromQuery] int? priority,
        [FromQuery] int? category,
        [FromQuery] Guid? assignedSupportAgentUserId,
        [FromQuery] bool? unassignedOnly,
        [FromQuery] bool? firstResponseOverdueOnly,
        [FromQuery] bool? resolutionOverdueOnly,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            ExportAdminSupportTicketsQuery.FromFilters(
                search, status, priority, category, assignedSupportAgentUserId,
                unassignedOnly, firstResponseOverdueOnly, resolutionOverdueOnly),
            cancellationToken);
        return result.ToSupportTicketCsvDownloadResult(this);
    }
}
