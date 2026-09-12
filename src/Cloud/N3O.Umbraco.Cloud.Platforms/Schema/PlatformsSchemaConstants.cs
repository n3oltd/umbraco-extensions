using System;

namespace N3O.Umbraco.Cloud.Platforms;

public static class PlatformsSchemaConstants {
    public const string ConfigurationSection = "Platforms";

    public static class Migrations {
        public const string PlanName = "N3O.Platforms.Schema";
        public const string PlatformsContentTypesMigrationV2 = "2026.08.31";
    }

    public static class Folders {
        public const string Calculator = "Calculator";
        public const string Campaigns = "Campaigns";
        public const string CrossSells = "Cross Sells";
        public const string CrowdfundingCampaigns = "Crowdfunding Campaigns";
        public const string DonationForms = "Donation Forms";
        public const string Offerings = "Offerings";
        public const string Platforms = "Platforms";
        public const string Qurbani = "Qurbani";
        public const string Zakat = "Zakat";
    }

    // Data type names are the adoption key on every existing site, so they must match what is deployed
    public static class DataTypes {
        public const string AnalyticsTagsList = "Platforms Analytics Tags List";
        public const string CampaignsMultiple = "Platforms Campaigns Data List (0, n)";
        public const string CampaignsSingle = "Platforms Campaigns Data List (0, 1)";
        public const string DonateButtonAction = "Platforms Donate Button Action Data List (0, 1)";
        public const string ECommerceStage = "Platforms Ecommerce Stage Data List (0, 1)";
        public const string ElementEmbedCodeLabel = "Platforms Element Embed Code Label";
        public const string QurbaniItem = "Platforms Qurbani Item Data List (0, 1)";
        public const string QurbaniSeasonCategoryPicker = "Platforms Qurbani Season Category Picker (0, n)";
        public const string SuggestedAmounts = "Nested Platforms Suggested Amount (0, 3)";
        public const string Summary = "Platforms Summary";
    }

    public static class DataTypeKeys {
        // The key the sites' uSync exports hold for the type, save Muslim Hands, whose own key is matched by name
        public static readonly Guid CampaignsSingle = new("d51913ab-a36d-4d15-9c5c-7876319e967e");
    }

    public static class SharedDataTypes {
        public const string ContentPicker = "Content Picker";
        public const string DateTime = "Date Picker with time";
        public const string DayOfMonth = "Day Of Month Data List (0, 1)";
        public const string DayOfWeek = "Day Of Week Data List (0, 1)";
        public const string DonationItem = "Donation Item Data List (0, 1)";
        public const string FeedbackScheme = "Feedback Scheme Data List (0, 1)";
        public const string FundDimension1 = "Fund Dimension 1 Value Data List (0, 1)";
        public const string FundDimension2 = "Fund Dimension 2 Value Data List (0, 1)";
        public const string FundDimension3 = "Fund Dimension 3 Value Data List (0, 1)";
        public const string FundDimension4 = "Fund Dimension 4 Value Data List (0, 1)";
        public const string GiftType = "Gift Type Data List (0, 1)";
        public const string GivingSchedule = "Giving Schedule Data List (0, 1)";
        public const string IconMediaPicker = "Icon Media Picker (0, 1)";
        public const string ImageMediaPicker = "Image Media Picker (0, 1)";
        public const string Markdown = "Markdown Text Editor";
        public const string Metal = "Metal Data List (0, 1)";
        public const string Money = "Money";
        public const string NisabType = "Nisab Type Data List (0, 1)";
        public const string OfferingPicker = "Platforms Offering Picker (0, 1)";
        public const string PageBlockGrid = "Page Block Grid";
        public const string PerplexBlocks = "Blocks";
        public const string RegularGivingFrequency = "Regular Giving Frequency Data List (0, 1)";
        public const string SponsorshipScheme = "Sponsorship Scheme Data List (0, 1)";
        public const string Textarea = "Textarea";
        public const string TextBox = "Textstring";
        public const string Toggle = "YesNo Toggle";
        public const string ZakatFieldClassification = "Zakat Calculator Field Classification Data List (0, 1)";
        public const string ZakatFieldType = "Zakat Calculator Field Type Data List (0, 1)";
    }

    public static class Groups {
        public const string Analytics = "Analytics";
        public const string Defaults = "Defaults";
        public const string Embed = "Embed";
        public const string Filters = "Filters";
        public const string General = "General";
        public const string Options = "Options";
        public const string Settings = "Settings";
        public const string Suggestions = "Suggestions";
    }

    public static class Descriptions {
        public const string CrowdfundingCampaignCampaign = "The campaign this crowdfunding campaign belongs to";
    }

    public static class Names {
        public const string CrowdfundingCampaign = "Crowdfunding Campaign";
        public const string CrowdfundingCampaignContentSyncStamp = "Content Sync Stamp";
        public const string CrowdfundingCampaigns = "Crowdfunding Campaigns";
    }

    public static class Tabs {
        public const string CrowdfundingCampaign = "Crowdfunding Campaign";
    }
}
