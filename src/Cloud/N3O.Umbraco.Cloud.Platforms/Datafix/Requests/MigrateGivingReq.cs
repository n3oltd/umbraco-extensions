using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class MigrateGivingReq {
    public int? Limit { get; set; }
    public bool IncludeCrossSells { get; set; } = true;

    // Supplying media means the migration never reaches out to a placeholder service.
    public Guid? DefaultIconMediaId { get; set; }
    public Guid? DefaultImageMediaId { get; set; }
    public Guid? DefaultHeroImageMediaId { get; set; }
}
