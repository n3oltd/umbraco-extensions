using N3O.Umbraco.Lookups;
using System.Globalization;

namespace N3O.Umbraco.Localization;

public class TimeFormat : NamedLookup {
    public TimeFormat(string id, string name, bool hasMeridiem, string cultureCode) : base(id, name) {
        HasMeridiem = hasMeridiem;
        CultureCode = cultureCode;
    }

    public bool HasMeridiem { get; }
    public string CultureCode { get; }

    public DateTimeFormatInfo GetDateTimeFormatInfo() {
        return CultureInfo.GetCultureInfo(CultureCode).DateTimeFormat;
    }
}

public class TimeFormats : StaticLookupsCollection<TimeFormat> {
    public static readonly TimeFormat _12 = new("12", "12 Hour Format", true, "en-US");
    public static readonly TimeFormat _24 = new("24", "24 Hour Format", false, "en-GB");
}
