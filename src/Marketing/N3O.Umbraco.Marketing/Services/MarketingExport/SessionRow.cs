using System;

namespace N3O.Umbraco.Marketing.Services;

public class SessionRow {
    public string Campaign { get; set; }
    public bool IsAnonymousVisitor { get; set; }
    public bool IsNewVisitor { get; set; }
    public string Medium { get; set; }
    public int Pageviews { get; set; }
    public string ReferrerDomain { get; set; }
    public long SessionId { get; set; }
    public string Source { get; set; }
    public DateTime Timestamp { get; set; }
    public long VisitorId { get; set; }
}
