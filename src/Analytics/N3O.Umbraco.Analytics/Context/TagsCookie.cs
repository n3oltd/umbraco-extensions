using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Context;
using Newtonsoft.Json.Linq;
using System;

namespace N3O.Umbraco.Analytics.Context;

public class TagsCookie : Cookie {
    public TagsCookie(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor) { }

    public JObject GetTags() {
        var jObject = default(JObject);

        try {
            jObject = JObject.Parse(GetValue());
        } catch {
            jObject = null;
        }

        return jObject;
    }

    protected override void SetOptions(CookieOptions cookieOptions) {
        base.SetOptions(cookieOptions);

        // Written from the browser, so it cannot be HttpOnly.
        cookieOptions.HttpOnly = false;
    }

    protected override string Name => AnalyticsConstants.Tags.Cookie.Name;
    protected override TimeSpan Lifetime => AnalyticsConstants.Tags.Cookie.Lifetime;
}
