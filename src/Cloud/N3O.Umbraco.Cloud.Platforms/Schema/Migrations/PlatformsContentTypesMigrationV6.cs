using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV6 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV6(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        LimitZakatCalculatorField();
        LimitZakatCalculatorSection();
    }

    private void LimitZakatCalculatorField() {
        var designer = ForExistingDocument<ZakatCalculatorFieldSettingsContent>();

        var general = designer.Group(Groups.General);

        general.TextBox(x => x.Alias).DataType(DataTypeNames.ZakatCalculatorAlias);
        general.TextBox(x => x.Tooltip).DataType(DataTypeNames.ZakatCalculatorTooltip);

        designer.Save();
    }

    private void LimitZakatCalculatorSection() {
        var designer = ForExistingDocument<ZakatCalculatorSectionSettingsContent>();

        var general = designer.Group(Groups.General);

        general.TextBox(x => x.Alias).DataType(DataTypeNames.ZakatCalculatorAlias);

        designer.Save();
    }
}
