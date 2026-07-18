using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Cms.Common;

public static class CmsSlugValidator
{
    public static bool IsValidCustomSlug(string slug) =>
        SlugHelper.IsValidSlug(slug) && !CmsSlugs.IsReserved(slug) && !CmsSlugs.SystemSlugs.Contains(slug);
}
