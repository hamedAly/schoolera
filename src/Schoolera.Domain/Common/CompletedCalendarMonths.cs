namespace Schoolera.Domain.Common;

/// <summary>
/// Pure completed-calendar-months calculator between a birth date and a reference date.
/// Age is always derived; never persist a mutable Age field on the child.
/// </summary>
public static class CompletedCalendarMonths
{
    /// <summary>
    /// Computes completed calendar months. Throws when <paramref name="birthDate"/> is after
    /// <paramref name="referenceDate"/>; callers that need a soft failure should use <see cref="TryCompute"/>.
    /// </summary>
    public static int Compute(DateOnly birthDate, DateOnly referenceDate)
    {
        if (!TryCompute(birthDate, referenceDate, out var months))
        {
            throw new ArgumentOutOfRangeException(
                nameof(birthDate),
                "Birth date cannot be after the reference date.");
        }

        return months;
    }

    /// <summary>
    /// Returns false when birth is after reference (InvalidBirthDate). Otherwise sets
    /// <paramref name="completedMonths"/> using anniversary-day and month-end rules.
    /// </summary>
    public static bool TryCompute(DateOnly birthDate, DateOnly referenceDate, out int completedMonths)
    {
        completedMonths = 0;
        if (birthDate > referenceDate)
        {
            return false;
        }

        var months = (referenceDate.Year - birthDate.Year) * 12
            + (referenceDate.Month - birthDate.Month);

        // Effective anniversary day in the reference month:
        // - Month-end: birth day 31 in a shorter month → last day of that month
        // - Feb 29 on non-leap years → Feb 28 (DaysInMonth)
        var daysInReferenceMonth = DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month);
        var anniversaryDay = Math.Min(birthDate.Day, daysInReferenceMonth);

        if (referenceDate.Day < anniversaryDay)
        {
            months -= 1;
        }

        completedMonths = months;
        return true;
    }
}
