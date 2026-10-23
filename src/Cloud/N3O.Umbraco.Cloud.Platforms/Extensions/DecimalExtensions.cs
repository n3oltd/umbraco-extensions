namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DecimalExtensions {
    public static bool IsSpecified(this decimal value) {
        return value != 0;
    }

    public static decimal ToFraction(this decimal percentage) {
        return percentage / 100;
    }
}
