namespace Schoolera.Application.Parent.Common;

public static class ParentChildAge
{
    public static int ComputeAgeYears(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public static bool IsSchoolAge(DateOnly birthDate, DateOnly today, int minAgeYears, int maxAgeYears)
    {
        if (birthDate >= today)
        {
            return false;
        }

        var age = ComputeAgeYears(birthDate, today);
        return age >= minAgeYears && age <= maxAgeYears;
    }
}
