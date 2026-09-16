namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationEnvironmentRes {
    public string EnvironmentName { get; set; }
    public bool IsDevelopment { get; set; }
    public bool EnableLiveTesting { get; set; }
    public string CampaignsWebhookUrl { get; set; }
    public string OfferingsWebhookUrl { get; set; }
    public string WebhookHost { get; set; }
    public bool IsLiveCloud { get; set; }
    public string SubscriptionId { get; set; }
}
