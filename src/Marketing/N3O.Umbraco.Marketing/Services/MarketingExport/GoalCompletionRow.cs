using System;

namespace N3O.Umbraco.Marketing.Services;

public class GoalCompletionRow {
    public string Campaign { get; set; }
    public string Medium { get; set; }
    public string Name { get; set; }
    public string ReferrerDomain { get; set; }
    public string Source { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
}
