using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV3 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV3(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddCrossSellRelativeValue();
        AddCrossSellTargetGiftTypes();
    }

    private void AddCrossSellRelativeValue() {
        var designer = ForExisting<CrossSellContent>();

        var general = designer.Group(Groups.General);

        general.Decimal(x => x.Percentage).DataType(DataTypeNames.Percentage);
        general.Decimal(x => x.Minimum).DataType(Shared.Money);
        general.Decimal(x => x.Maximum).DataType(Shared.Money);
        general.Decimal(x => x.Threshold).DataType(Shared.Money);

        designer.Save();
    }

    private void AddCrossSellTargetGiftTypes() {
        var designer = ForExisting<CrossSellContent>();

        designer.Group(Groups.Options)
                .ContentmentDataList(x => x.TargetGiftTypes)
                .DataType(DataTypeNames.GiftTypesMultiple);

        designer.Save();
    }
}
