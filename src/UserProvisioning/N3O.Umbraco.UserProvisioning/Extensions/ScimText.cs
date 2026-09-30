using System;

namespace N3O.Umbraco.UserProvisioning.Extensions;

public static class ScimText {
    public static bool Is(this string value, string keyword) {
        return string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase);
    }

    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;

    public static StringComparison Comparison => StringComparison.OrdinalIgnoreCase;
}
