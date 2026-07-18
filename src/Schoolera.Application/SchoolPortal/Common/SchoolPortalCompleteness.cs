using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

public static class SchoolPortalCompleteness
{
    public static int CalculatePercent(SchoolDashboardCounts counts)
    {
        const int totalChecks = 10;
        var completed = 0;

        if (counts.HasLogo)
        {
            completed++;
        }

        if (counts.HasCover)
        {
            completed++;
        }

        if (counts.HasShortDescription)
        {
            completed++;
        }

        if (counts.HasFullDescription)
        {
            completed++;
        }

        if (counts.HasPublicContact)
        {
            completed++;
        }

        if (counts.ActiveBranchCount > 0)
        {
            completed++;
        }

        if (counts.MainBranchCount > 0)
        {
            completed++;
        }

        if (counts.ActiveOfferingCount > 0)
        {
            completed++;
        }

        if (counts.ActiveTuitionFeeCount > 0)
        {
            completed++;
        }

        if (counts.GalleryImageCount > 0)
        {
            completed++;
        }

        return (int)Math.Round(completed * 100m / totalChecks, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> BuildWarnings(SchoolStatus status, SchoolDashboardCounts counts)
    {
        var warnings = new List<string>();

        if (status == SchoolStatus.Suspended)
        {
            warnings.Add("schoolPortal.warnings.suspended");
        }

        if (!counts.HasLogo)
        {
            warnings.Add("schoolPortal.warnings.missingLogo");
        }

        if (!counts.HasCover)
        {
            warnings.Add("schoolPortal.warnings.missingCover");
        }

        if (counts.MainBranchCount == 0)
        {
            warnings.Add("schoolPortal.warnings.missingMainBranch");
        }

        if (counts.ActiveOfferingCount == 0)
        {
            warnings.Add("schoolPortal.warnings.noActiveOfferings");
        }

        if (counts.ActiveTuitionFeeCount == 0)
        {
            warnings.Add("schoolPortal.warnings.noActiveTuitionFees");
        }

        if (!counts.HasPublicContact)
        {
            warnings.Add("schoolPortal.warnings.missingPublicContact");
        }

        return warnings;
    }
}
