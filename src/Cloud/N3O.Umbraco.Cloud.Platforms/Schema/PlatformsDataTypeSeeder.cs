using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using System;
using Umbraco.Cms.Core.Configuration;
using DataTypeKeys = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypeKeys;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;
using QurbaniItemDataSource = N3O.Umbraco.Giving.Allocations.Lookups.QurbaniItemDataSource;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsDataTypeSeeder : IPlatformsDataTypeSeeder {
    // Nested content was removed in Umbraco 14, where the block list replaces it
    private const int BlockListFromUmbracoMajor = 14;

    private const string EmbedCodeTemplate = "<label>{{model.value}}</label>";

    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly ILogger<PlatformsDataTypeSeeder> _logger;
    private readonly IUmbracoVersion _umbracoVersion;

    public PlatformsDataTypeSeeder(IDataTypeEditor dataTypeEditor,
                                   ILogger<PlatformsDataTypeSeeder> logger,
                                   IUmbracoVersion umbracoVersion) {
        _dataTypeEditor = dataTypeEditor;
        _logger = logger;
        _umbracoVersion = umbracoVersion;
    }

    public void Seed() {
        Seed(DataTypeNames.AnalyticsTagsList, SeedAnalyticsTagsList);
        Seed(DataTypeNames.CampaignsMultiple, SeedCampaigns);
        Seed(DataTypeNames.CampaignsSingle, SeedCampaign);
        Seed(DataTypeNames.DonateButtonAction, SeedDonateButtonAction);
        Seed(DataTypeNames.DonationFormCampaign, SeedDonationFormCampaign);
        Seed(DataTypeNames.DonationFormOffering, SeedDonationFormOffering);
        Seed(DataTypeNames.ECommerceStage, SeedECommerceStage);
        Seed(DataTypeNames.ElementEmbedCodeLabel, SeedElementEmbedCodeLabel);
        Seed(DataTypeNames.QurbaniItem, SeedQurbaniItem);
        Seed(DataTypeNames.QurbaniSeasonCategoryPicker, SeedQurbaniSeasonCategoryPicker);
        Seed(DataTypeNames.SuggestedAmounts, SeedSuggestedAmounts);
        Seed(DataTypeNames.Summary, SeedSummary);
    }

    // Kept out of Seed because the block list variant stores the element types by key and so cannot be created
    // before they exist, which is only true once the content type seeder has run
    public void SeedDonationFormList() {
        Seed(DataTypeNames.DonationFormList,
             _umbracoVersion.Version.Major >= BlockListFromUmbracoMajor
                 ? SeedDonationFormBlockList
                 : SeedDonationFormNestedContent);
    }

    private void Seed(string name, Action seed) {
        if (_dataTypeEditor.Find(name) != null) {
            return;
        }

        try {
            seed();
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not create platforms data type {Name}", name);
        }
    }

    private void SeedAnalyticsTagsList() {
        var designer = _dataTypeEditor.NewContentmentListItems(DataTypeNames.AnalyticsTagsList);

        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.AnalyticsTagsList);

        designer.Save();
    }

    private void SeedCampaign() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.CampaignsSingle);

        designer.DataSource<CampaignDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms);
        designer.WithId(DataTypeKeys.CampaignsSingle);

        designer.Save();
    }

    private void SeedCampaigns() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.CampaignsMultiple);

        designer.DataSource<CampaignDataSource>();
        designer.AllowMultiple();
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.CampaignsMultiple);

        designer.Save();
    }

    private void SeedDonateButtonAction() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.DonateButtonAction);

        designer.DataSource<DonationButtonActionDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.DonateButtonAction);

        designer.Save();
    }

    private void SeedDonationFormBlockList() {
        var designer = _dataTypeEditor.NewBlockList(DataTypeNames.DonationFormList);

        designer.AllowBlocks(PlatformsConstants.DonationFormItems.Campaign,
                             PlatformsConstants.DonationFormItems.Offering);
        designer.Limit(0, 1);
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithId(DataTypeKeys.DonationFormList);

        designer.Save();
    }

    private void SeedDonationFormCampaign() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.DonationFormCampaign);

        designer.DataSource<DonationFormCampaignElementKindDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithId(DataTypeKeys.DonationFormCampaign);

        designer.Save();
    }

    private void SeedDonationFormNestedContent() {
        var designer = _dataTypeEditor.NewNestedContent(DataTypeNames.DonationFormList);

        designer.AddElementType(PlatformsConstants.DonationFormItems.Campaign);
        designer.AddElementType(PlatformsConstants.DonationFormItems.Offering);
        designer.Limit(0, 1);
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithId(DataTypeKeys.DonationFormList);

        designer.Save();
    }

    private void SeedDonationFormOffering() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.DonationFormOffering);

        designer.DataSource<DonationFormOfferingElementKindDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithId(DataTypeKeys.DonationFormOffering);

        designer.Save();
    }

    private void SeedECommerceStage() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.ECommerceStage);

        designer.DataSource<ECommerceStageDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.ECommerceStage);

        designer.Save();
    }

    private void SeedElementEmbedCodeLabel() {
        var designer = _dataTypeEditor.NewContentmentTemplatedLabel(DataTypeNames.ElementEmbedCodeLabel);

        designer.Template(EmbedCodeTemplate);
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }

    private void SeedQurbaniItem() {
        var designer = _dataTypeEditor.NewContentmentDataList(DataTypeNames.QurbaniItem);

        designer.DataSource<QurbaniItemDataSource>();
        designer.Limit(1);
        designer.InFolder(Folders.Platforms, Folders.Qurbani);
        designer.WithDeterministicId(DataTypeNames.QurbaniItem);

        designer.Save();
    }

    private void SeedQurbaniSeasonCategoryPicker() {
        var designer = _dataTypeEditor.NewMultiNodeTreePicker(DataTypeNames.QurbaniSeasonCategoryPicker);

        designer.AllowContentTypes(PlatformsConstants.Qurbani.Season.Category.Alias);
        designer.Limit(0, 0);
        designer.InFolder(Folders.Platforms, Folders.Qurbani);
        designer.WithDeterministicId(DataTypeNames.QurbaniSeasonCategoryPicker);

        designer.Save();
    }

    private void SeedSuggestedAmounts() {
        var designer = _dataTypeEditor.NewNestedContent(DataTypeNames.SuggestedAmounts);

        designer.ElementType<DonationFormStateSuggestedAmountElement>();
        designer.Limit(0, 3);
        designer.NameTemplate("{{amount}} {{description}}");
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId(DataTypeNames.SuggestedAmounts);

        designer.Save();
    }

    private void SeedSummary() {
        var designer = _dataTypeEditor.NewTextarea(DataTypeNames.Summary);

        designer.MaxChars(200);
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(DataTypeNames.Summary);

        designer.Save();
    }
}
