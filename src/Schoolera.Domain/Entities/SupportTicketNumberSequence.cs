namespace Schoolera.Domain.Entities;

/// <summary>
/// Day-scoped counter for race-safe ticket references (ST-{yyyyMMdd}-{sequence:D5}).
/// Updated under a SQL Server UPDLOCK/ROWLOCK transaction.
/// </summary>
public sealed class SupportTicketNumberSequence
{
    private SupportTicketNumberSequence()
    {
    }

    public SupportTicketNumberSequence(string dayKey)
    {
        DayKey = dayKey.Trim();
        LastValue = 0;
    }

    /// <summary>UTC day key in yyyyMMdd form.</summary>
    public string DayKey { get; private set; } = string.Empty;

    public long LastValue { get; private set; }

    public long Next()
    {
        LastValue++;
        return LastValue;
    }
}
