using Slugify;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class SlugHelperExtensions {
    public static string GeneratePlatformsSlug(this ISlugHelper slugHelper, string name) {
        return slugHelper.GenerateSlug(name);
    }
}
