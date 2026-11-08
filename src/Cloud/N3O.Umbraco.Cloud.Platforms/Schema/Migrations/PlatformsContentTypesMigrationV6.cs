using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Descriptions = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Descriptions;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Names = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Names;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV6 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV6(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddCampaignTags();
    }

    private void AddCampaignTags() {
        var designer = ForExistingDocument<CampaignContent>();

        var tags = designer.Group(Groups.Tags);

        tags.CampaignTags(x => x.Tags)
            .Name(Names.CampaignTags)
            .DataType(DataTypeNames.CampaignTags)
            .Description(Descriptions.CampaignTags);

        tags.ContentmentDataList(x => x.Fundraiser)
            .DataType(DataTypeNames.FundraisersSingle)
            .Description(Descriptions.CampaignFundraiser);

        designer.Save();
    }
}
