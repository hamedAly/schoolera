namespace Schoolera.Domain.Entities;

/// <summary>
/// Year-scoped counter for race-safe application numbers (APP-{year}-{sequence:D6}).
/// Updated under a SQL Server UPDLOCK/ROWLOCK transaction — not an in-memory counter.
/// </summary>
public sealed class AdmissionApplicationNumberSequence
{
    private AdmissionApplicationNumberSequence()
    {
    }

    public AdmissionApplicationNumberSequence(int year)
    {
        Year = year;
        LastValue = 0;
    }

    public int Year { get; private set; }

    public long LastValue { get; private set; }

    public long Next()
    {
        LastValue++;
        return LastValue;
    }
}
