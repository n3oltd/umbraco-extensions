using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsContentTypeSeeder {
    private void SeedPlatforms() {
        var designer = _contentTypeEditor.NewDocument<PlatformsContent>();

        designer.SetName("Platforms");
        designer.SetIcon("icon-iphone color-black");
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId();
        designer.AllowAtRoot();

        designer.Save();
    }

    private void SeedGivingCampaign() {
        var designer = _contentTypeEditor.NewDocument<GivingCampaignContent>();

        designer.SetName("Giving Campaign");
        designer.SetIcon("icon-donate color-black");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();

        designer.Save();
    }

    private void SeedQurbaniSeason() {
        var designer = _contentTypeEditor.NewDocument<QurbaniSeasonContent>();

        designer.SetName("Qurbani Season");
        designer.SetIcon("icon-calendar-alt color-black");
        designer.InFolder(Folders.Platforms, Folders.Qurbani);
        designer.WithDeterministicId();
        designer.AllowChildren(PlatformsConstants.Qurbani.Season.Category.Alias);

        designer.Save();
    }

    private void SeedStandardCampaign() {
        var designer = _contentTypeEditor.NewDocument<StandardCampaignContent>();

        designer.SetName("Standard Campaign");
        designer.SetIcon("icon-files color-black");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();
        designer.AddComposition<CampaignContent>();
        designer.AddComposition<DonationFormContentContent>();

        designer.Save();
    }

    private void SeedCampaign() {
        var designer = _contentTypeEditor.NewDocument<CampaignContent>();

        designer.SetName("Campaign");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.Decimal(x => x.Target).DataType(Shared.Money);
        general.Textarea(x => x.Notes).DataType(Shared.Textarea);

        var embed = designer.Group(Groups.Embed);

        embed.ContentmentTemplatedLabel(x => x.DonationFormEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);
        embed.ContentmentTemplatedLabel(x => x.DonationButtonEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);
        embed.ContentmentTemplatedLabel(x => x.DonationPopupEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }

    private void SeedCrossSell() {
        var designer = _contentTypeEditor.NewDocument<CrossSellContent>();

        designer.SetName("Cross-Sell");
        designer.InFolder(Folders.Platforms, Folders.CrossSells);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.ContentmentDataList(x => x.Stage).DataType(DataTypeNames.ECommerceStage).Mandatory();
        general.Decimal(x => x.Amount).DataType(Shared.Money);

        designer.Group(Groups.Options)
                .ContentmentDataList(x => x.TargetCampaigns)
                .DataType(DataTypeNames.CampaignsMultiple);

        designer.Save();
    }

    private void SeedCrowdfunder() {
        var designer = _contentTypeEditor.NewDocument<CrowdfunderContent>();

        designer.SetName("Crowdfunder");
        designer.SetIcon("icon-target color-black");
        designer.InFolder(Folders.Platforms, Folders.Crowdfunders);
        designer.WithDeterministicId();

        designer.Tab("Crowdfunder")
                .ContentPicker(x => x.Campaign)
                .DataType(Shared.ContentPicker)
                .Mandatory()
                .Description("The campaign this crowdfunder raises for");

        designer.Save();
    }

    private void SeedCrowdfunders() {
        var designer = _contentTypeEditor.NewDocument("Crowdfunders", PlatformsConstants.CrowdfundingCampaigns.Alias);

        designer.SetIcon("icon-file-cabinet color-black");
        designer.InFolder(Folders.Platforms, Folders.Crowdfunders);
        designer.WithDeterministicId();
        designer.AllowChildren(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias);

        designer.Save();
    }

    private void SeedOffering() {
        var designer = _contentTypeEditor.NewDocument<OfferingContent>();

        designer.SetName("Offering");
        designer.InFolder(Folders.Platforms, Folders.Offerings);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.Toggle(x => x.AllowCrowdfunding).DataType(Shared.Toggle);
        general.Textarea(x => x.Notes).DataType(Shared.Textarea);

        var embed = designer.Group(Groups.Embed);

        embed.ContentmentTemplatedLabel(x => x.DonationFormEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);
        embed.ContentmentTemplatedLabel(x => x.DonationButtonEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);
        embed.ContentmentTemplatedLabel(x => x.DonationPopupEmbedCode).DataType(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }

    private void SeedQurbaniCampaign() {
        var designer = _contentTypeEditor.NewDocument<QurbaniCampaignContent>();

        designer.SetName("Qurbani Campaign");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();
        designer.AddComposition<CampaignContent>();
        designer.AddComposition<DonationFormContentContent>();

        var options = designer.Group(Groups.Options);

        options.DateTime(x => x.BeginAt).DataType(Shared.DateTime).Mandatory();
        options.DateTime(x => x.EndAt).DataType(Shared.DateTime).Mandatory();

        designer.Save();
    }

    private void SeedQurbaniSeasonCategory() {
        var designer = _contentTypeEditor.NewDocument<QurbaniSeasonCategoryContent>();

        designer.SetName("Qurbani Season Category");
        designer.InFolder(Folders.Platforms, Folders.Qurbani);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.MediaPicker(x => x.Icon).DataType(Shared.IconMediaPicker);
        general.TextBox(x => x.Summary).DataType(Shared.TextBox);

        designer.Save();
    }

    private void SeedRegularGivingCampaign() {
        var designer = _contentTypeEditor.NewDocument<RegularGivingCampaignContent>();

        designer.SetName("Regular Giving Campaign");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();
        designer.AddComposition<CampaignContent>();
        designer.AddComposition<DonationFormContentContent>();
        designer.AddComposition<GivingCampaignContent>();

        designer.Group(Groups.General)
                .ContentmentDataList(x => x.RegularGivingFrequency)
                .DataType(Shared.RegularGivingFrequency)
                .Mandatory();

        var options = designer.Group(Groups.Options);

        options.ContentmentDataList(x => x.DayOfMonth).DataType(Shared.DayOfMonth);
        options.ContentmentDataList(x => x.DayOfWeek).DataType(Shared.DayOfWeek);

        designer.Save();
    }

    private void SeedScheduledGivingCampaign() {
        var designer = _contentTypeEditor.NewDocument<ScheduledGivingCampaignContent>();

        designer.SetName("Scheduled Giving Campaign");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();
        designer.AddComposition<CampaignContent>();
        designer.AddComposition<DonationFormContentContent>();
        designer.AddComposition<GivingCampaignContent>();

        designer.Group(Groups.General)
                .ContentmentDataList(x => x.Schedule)
                .DataType(Shared.GivingSchedule)
                .Mandatory();

        designer.Save();
    }

    private void SeedTelethonCampaign() {
        var designer = _contentTypeEditor.NewDocument<TelethonCampaignContent>();

        designer.SetName("Telethon Campaign");
        designer.InFolder(Folders.Platforms, Folders.Campaigns);
        designer.WithDeterministicId();
        designer.AddComposition<CampaignContent>();
        designer.AddComposition<DonationFormContentContent>();

        var options = designer.Group(Groups.Options);

        options.DateTime(x => x.BeginAt).DataType(Shared.DateTime).Mandatory();
        options.DateTime(x => x.EndAt).DataType(Shared.DateTime).Mandatory();

        designer.Save();
    }
}
