using System.Globalization;
using System.Text;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SupportTickets.Common;

/// <summary>UTF-8 CSV export for Platform Admin support tickets (no message bodies).</summary>
public static class SupportTicketCsvExporter
{
    public const int MaxExportRows = 5000;

    private static readonly string[] Headers =
    [
        "Reference",
        "Subject",
        "Status",
        "Priority",
        "Category",
        "ParentUserId",
        "AssignedSupportAgentUserId",
        "AdmissionApplicationId",
        "SourceContactRequestId",
        "CreatedAtUtc",
        "UpdatedAtUtc",
        "FirstResponseAtUtc",
        "ResolvedAtUtc",
        "ClosedAtUtc",
        "FirstResponseDueAtUtc",
        "ResolutionDueAtUtc",
        "IsFirstResponseOverdue",
        "IsResolutionOverdue",
    ];

    public static SupportTicketExportFileDto Build(
        IReadOnlyList<SupportTicket> tickets,
        DateTimeOffset? nowUtc = null)
    {
        var utcNow = nowUtc ?? DateTimeOffset.UtcNow;
        var stamp = utcNow.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var fileName = $"support-tickets-{stamp}.csv";

        var builder = new StringBuilder();
        builder.Append(string.Join(',', Headers));
        builder.Append("\r\n");

        foreach (var ticket in tickets.Take(MaxExportRows))
        {
            builder.Append(Escape(ticket.Reference));
            builder.Append(',');
            builder.Append(Escape(ticket.Subject));
            builder.Append(',');
            builder.Append(Escape(ticket.Status.ToString()));
            builder.Append(',');
            builder.Append(Escape(ticket.Priority.ToString()));
            builder.Append(',');
            builder.Append(Escape(ticket.Category.ToString()));
            builder.Append(',');
            builder.Append(Escape(ticket.ParentUserId.ToString("D")));
            builder.Append(',');
            builder.Append(Escape(ticket.AssignedSupportAgentUserId?.ToString("D")));
            builder.Append(',');
            builder.Append(Escape(ticket.AdmissionApplicationId?.ToString("D")));
            builder.Append(',');
            builder.Append(Escape(ticket.SourceContactRequestId?.ToString("D")));
            builder.Append(',');
            builder.Append(Escape(ticket.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.FirstResponseAtUtc?.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.ResolvedAtUtc?.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.ClosedAtUtc?.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.FirstResponseDueAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(ticket.ResolutionDueAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(SupportTicketTransitionPolicy.IsFirstResponseOverdue(
                ticket.FirstResponseAtUtc,
                ticket.FirstResponseDueAtUtc,
                utcNow).ToString()));
            builder.Append(',');
            builder.Append(Escape(SupportTicketTransitionPolicy.IsResolutionOverdue(
                ticket.Status,
                ticket.ResolutionDueAtUtc,
                utcNow).ToString()));
            builder.Append("\r\n");
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var body = utf8.GetBytes(builder.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var content = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
        Buffer.BlockCopy(body, 0, content, bom.Length, body.Length);
        return new SupportTicketExportFileDto(content, fileName);
    }

    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            text = "'" + text;
        }

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
