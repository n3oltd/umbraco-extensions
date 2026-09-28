using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class PlatformsLanguage : NamedLookup {
    public PlatformsLanguage(string id, string name, bool isRightToLeft, PlatformsLanguage parent)
        : base(id, name) {
        IsRightToLeft = isRightToLeft;
        Parent = parent;
    }

    public bool IsRightToLeft { get; }
    public PlatformsLanguage Parent { get; }
}

public class PlatformsLanguages : StaticLookupsCollection<PlatformsLanguage> {
    public static readonly PlatformsLanguage Arabic = new("ar", "Arabic", true, null);
    public static readonly PlatformsLanguage English = new("en", "English (United Kingdom)", false, null);
    public static readonly PlatformsLanguage EnglishCanada = new("en-CA", "English (Canada)", false, English);
    public static readonly PlatformsLanguage EnglishUnitedStates = new("en-US", "English (United States)", false, English);
    public static readonly PlatformsLanguage French = new("fr", "French", false, null);
}
