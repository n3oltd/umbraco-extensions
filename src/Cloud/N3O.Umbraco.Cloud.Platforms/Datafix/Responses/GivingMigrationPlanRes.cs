using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationPlanRes {
    public GivingMigrationSummaryRes Summary { get; set; }
    public IReadOnlyList<GivingMigrationCampaignRes> Campaigns { get; set; } = [];
    public IReadOnlyList<GivingMigrationIssueRes> Blockers { get; set; } = [];
    public IReadOnlyList<GivingMigrationIssueRes> Warnings { get; set; } = [];
    public IReadOnlyList<GivingMigrationIssueRes> DataLoss { get; set; } = [];
}
