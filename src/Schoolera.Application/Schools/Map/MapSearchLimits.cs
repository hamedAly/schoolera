namespace Schoolera.Application.Schools.Map;

/// <summary>Server-controlled map search limits (not overridable by Angular beyond clamp).</summary>
public static class MapSearchLimits
{
    public const int AbsoluteMaxPins = 200;

    public const int DefaultMaxPins = 100;

    public const double MaxRadiusKm = 50d;

    /// <summary>Maximum north−south or east−west span in degrees for a single bbox request.</summary>
    public const double MaxBoundingBoxSpanDegrees = 2.5d;

    public static int ClampRequestedMaxPins(int? requested) =>
        Math.Clamp(requested ?? DefaultMaxPins, 1, AbsoluteMaxPins);
}
