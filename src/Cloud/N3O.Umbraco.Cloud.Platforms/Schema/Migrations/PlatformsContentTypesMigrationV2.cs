using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV2 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV2(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddCampaignPopupEmbedCode();
        AddCrossSellAmount();
        AddOfferingPopupEmbedCode();
    }

    private void AddCampaignPopupEmbedCode() {
        var designer = ForExistingDocument<CampaignContent>();

        designer.Group(Groups.Embed)
                .ContentmentTemplatedLabel(x => x.DonationPopupEmbedCode)
                .DataType(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }

    private void AddCrossSellAmount() {
        var designer = ForExistingDocument<CrossSellContent>();

        designer.Group(Groups.General).Decimal(x => x.Amount).DataType(Shared.Money);

        designer.Save();
    }

    private void AddOfferingPopupEmbedCode() {
        var designer = ForExistingDocument<OfferingContent>();

        designer.Group(Groups.Embed)
                .ContentmentTemplatedLabel(x => x.DonationPopupEmbedCode)
                .DataType(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }
}
