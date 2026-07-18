namespace Schoolera.Application.SupportTickets.Options;

/// <summary>
/// Development / non-production SLA foundation. Product SLA durations are unresolved;
/// do not treat these defaults as approved production values.
/// Bound to configuration section <c>SupportTickets:Sla</c>.
/// </summary>
public sealed class SupportTicketSlaOptions
{
    public const string SectionName = "SupportTickets:Sla";

    /// <summary>Parent reopen window from Resolved → Open (hours).</summary>
    public int ReopenWindowHours { get; set; } = 72;

    public int FirstResponseHoursUrgent { get; set; } = 2;

    public int FirstResponseHoursHigh { get; set; } = 4;

    public int FirstResponseHoursNormal { get; set; } = 24;

    public int FirstResponseHoursLow { get; set; } = 48;

    public int ResolutionHoursUrgent { get; set; } = 24;

    public int ResolutionHoursHigh { get; set; } = 48;

    public int ResolutionHoursNormal { get; set; } = 120;

    public int ResolutionHoursLow { get; set; } = 240;

    public int GetFirstResponseHours(int priority) =>
        priority switch
        {
            4 => FirstResponseHoursUrgent,
            3 => FirstResponseHoursHigh,
            1 => FirstResponseHoursLow,
            _ => FirstResponseHoursNormal,
        };

    public int GetResolutionHours(int priority) =>
        priority switch
        {
            4 => ResolutionHoursUrgent,
            3 => ResolutionHoursHigh,
            1 => ResolutionHoursLow,
            _ => ResolutionHoursNormal,
        };

    public (DateTimeOffset FirstResponseDue, DateTimeOffset ResolutionDue) ComputeDueDates(
        int priority,
        DateTimeOffset createdAtUtc)
    {
        var first = createdAtUtc.AddHours(GetFirstResponseHours(priority));
        var resolution = createdAtUtc.AddHours(GetResolutionHours(priority));
        return (first, resolution);
    }
}
