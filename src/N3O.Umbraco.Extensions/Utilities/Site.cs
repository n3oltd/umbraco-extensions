using N3O.Umbraco.Constants;
using N3O.Umbraco.Hosting;

namespace N3O.Umbraco.Utilities;

public static class Site {
    public static string Environment => EnvironmentData.GetOurValue(EnvironmentVariables.Environment);
    public static string Id => EnvironmentData.GetOurValue(EnvironmentVariables.SiteId);
    public static string Language => EnvironmentData.GetOurValue(EnvironmentVariables.SiteLanguage);
}