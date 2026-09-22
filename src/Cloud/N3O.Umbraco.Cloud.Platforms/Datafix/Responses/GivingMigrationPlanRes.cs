using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationPlanRes {
    public GivingMigrationSummaryRes Summary { get; set; }
    public IEnumerable<GivingMigrationCampaignRes> Campaigns { get; set; } = [];
    public IEnumerable<GivingMigrationCrossSellRes> CrossSells { get; set; } = [];
    public IEnumerable<GivingMigrationIssueRes> Blockers { get; set; } = [];
    public IEnumerable<GivingMigrationIssueRes> Warnings { get; set; } = [];
    public IEnumerable<GivingMigrationIssueRes> DataLoss { get; set; } = [];
}
