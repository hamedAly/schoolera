namespace Schoolera.Application.Common.Models;

/// <summary>
/// Stable taxonomy error codes. Frontends branch on these, never on localized text.
/// </summary>
public static class TaxonomyErrorCodes
{
    public const string NotFound = "taxonomy.not_found";
    public const string SlugDuplicate = "taxonomy.slug_duplicate";
    public const string CityNotFound = "taxonomy.city_not_found";
    public const string StageNotFound = "taxonomy.stage_not_found";
    public const string InvalidDateRange = "taxonomy.invalid_date_range";
    public const string CountryNotFound = "taxonomy.country_not_found";
    public const string GovernorateNotFound = "taxonomy.governorate_not_found";
    public const string HierarchyMismatch = "taxonomy.hierarchy_mismatch";
    public const string CodeDuplicate = "taxonomy.code_duplicate";
}
