using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class MigrateGivingReq {
    public int? Limit { get; set; }
    public bool IncludeCrossSells { get; set; } = true;

    // Media from the site's own library. Supply it and the migration never calls out.
    public Guid? DefaultIconMediaId { get; set; }
    public Guid? DefaultImageMediaId { get; set; }
    public Guid? DefaultHeroImageMediaId { get; set; }

    // Downloading a placeholder puts a third party's image on the client's live campaign pages, so it is asked for
    // rather than being what happens when the media above is left out.
    public bool AllowPlaceholderMedia { get; set; }
}
