using N3O.Umbraco.Templates.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class PlatformsTemplateConstants {
    public static class ModelKeys {
        public static readonly string Campaigns = nameof(Campaigns).ToModelKey();
        public static readonly string CampaignOfferings = nameof(CampaignOfferings).ToModelKey();
        public static readonly string CrowdfunderCampaignPage = nameof(CrowdfunderCampaignPage).ToModelKey();
        public static readonly string Nisab = nameof(Nisab).ToModelKey();
        public static readonly string User = nameof(User).ToModelKey();
    }
}
