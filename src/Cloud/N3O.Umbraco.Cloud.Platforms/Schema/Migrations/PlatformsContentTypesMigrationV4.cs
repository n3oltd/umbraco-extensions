using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV4 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV4(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddCrossSellRelativeValue();
        AddCrossSellTargeting();
    }

    private void AddCrossSellRelativeValue() {
        var designer = ForExistingDocument<CrossSellContent>();

        var general = designer.Group(Groups.General);

        general.Decimal(x => x.Percentage).Name("Percentage of Donation").DataType(DataTypeNames.Percentage);
        general.Decimal(x => x.Minimum).Name("Minimum Percentage Amount").DataType(Shared.Money);
        general.Decimal(x => x.Maximum).Name("Maximum Percentage Amount").DataType(Shared.Money);

        designer.Save();
    }

    private void AddCrossSellTargeting() {
        var designer = ForExistingDocument<CrossSellContent>();

        var options = designer.Group(Groups.Options);

        options.ContentmentDataList(x => x.TargetGiftTypes).DataType(DataTypeNames.GiftTypesMultiple);
        options.Decimal(x => x.TargetThreshold).Name("Show When Donation Reaches").DataType(Shared.Money);

        designer.Save();
    }
}
