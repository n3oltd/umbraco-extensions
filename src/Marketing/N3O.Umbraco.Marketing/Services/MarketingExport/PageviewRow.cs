using System;

namespace N3O.Umbraco.Marketing.Services;

public class PageviewRow {
    public bool IsEntrance { get; set; }
    public string Path { get; set; }
    public DateTime Timestamp { get; set; }
}
