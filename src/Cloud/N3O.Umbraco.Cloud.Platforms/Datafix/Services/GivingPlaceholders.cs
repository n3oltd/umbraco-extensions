using System;

namespace N3O.Umbraco.Cloud.Platforms;

public class GivingPlaceholders {
    public Guid MediaId { get; set; }
    public string MediaFileId { get; set; }
    public string Filename { get; set; }
    public string Src { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string AnalyticsTagsJson { get; set; }
}
