using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Descriptions = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Descriptions;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Names = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Names;
using TagKeys = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.TagKeys;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV6 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV6(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddCampaignFundraiserTag();
    }

    private void AddCampaignFundraiserTag() {
        var designer = ForExistingDocument<CampaignContent>();

        var tags = designer.Group(Groups.Tags);

        tags.ContentmentTextInput(CampaignTagAlias.For(TagKeys.Fundraiser))
            .Name(Names.Fundraiser)
            .DataType(DataTypeNames.FundraiserTag)
            .Description(Descriptions.CampaignFundraiser);

        designer.Save();
    }
}
