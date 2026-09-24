using System;

namespace N3O.Umbraco.UserProvisioning.Extensions;

public static class ScimText {
    // Collation would treat an embedded ignorable character as absent and read the result as the keyword
    // itself. Case is still ignored, because the provisioning service capitalises the patch operations
    public static bool Is(this string value, string keyword) {
        return string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase);
    }

    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;

    public static StringComparison Comparison => StringComparison.OrdinalIgnoreCase;
}
