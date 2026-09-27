using N3O.Umbraco.Attributes;
using System;

namespace N3O.Umbraco.Marketing.Models;

public class SessionReq {
    [Name("Referrer")]
    public Uri Referrer { get; set; }

    [Name("URL")]
    public Uri Url { get; set; }
}
