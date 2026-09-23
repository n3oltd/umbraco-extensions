using System;

namespace N3O.Umbraco.UserProvisioning.Scim;

public static class ScimText {
    // A protocol keyword is compared without collation, because a linguistic comparison treats some
    // characters as ignorable and would read a keyword with one embedded as the keyword itself. It stays
    // case insensitive because the provisioning service sends Add, Replace and Remove capitalised unless
    // the compliance flag is set on its tenant URL
    public static bool Is(this string value, string keyword) {
        return string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase);
    }

    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;

    public static StringComparison Comparison => StringComparison.OrdinalIgnoreCase;
}
