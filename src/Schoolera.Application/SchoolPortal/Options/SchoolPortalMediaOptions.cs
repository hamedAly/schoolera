namespace Schoolera.Application.SchoolPortal.Options;

public sealed class SchoolPortalMediaOptions
{
    public const string SectionName = "SchoolPortalMedia";

    public const string LogosCategory = "school-portal/logos";

    public const string CoversCategory = "school-portal/covers";

    public const string GalleryCategory = "school-portal/gallery";

    public long MaxLogoBytes { get; set; } = 2 * 1024 * 1024;

    public long MaxCoverBytes { get; set; } = 5 * 1024 * 1024;

    public long MaxGalleryImageBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxGeneralGalleryImages { get; set; } = 20;

    public int MaxImagesPerStage { get; set; } = 5;
}
