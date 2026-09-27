using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Hosting;

public static class EdgeCaching {
    private static readonly string PreventedKey = $"{nameof(EdgeCaching)}.Prevented";

    public static string GetHtmlTag(string host) {
        return $"html:{host}";
    }

    public static bool IsEnabled() {
        return EnvironmentData.GetOurValue(HostingConstants.Environment.Keys.EdgeCaching).EqualsInvariant("enabled");
    }

    public static bool IsPrevented(HttpContext context) {
        return context.Items.ContainsKey(PreventedKey);
    }

    // Marks a response that must not be shared through the edge cache, for content rendered from something the
    // origin cannot vouch for across visitors or across its edge lifetime.
    public static void Prevent(HttpContext context) {
        context.Items[PreventedKey] = true;
    }
}
