using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV3 : PlatformsContentTypesMigration {
    private static readonly string TitleAlias = AliasHelper<DonationFormStateSuggestedAmountElement>.PropertyAlias(x => x.Title);

    public PlatformsContentTypesMigrationV3(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddLegacySuggestedAmountTitle();
        AddSuggestedAmountTitle();
    }

    // A site seeded before the element was renamed still lists the old alias in its suggested amounts data type,
    // because the seeder never updates an existing data type.
    private void AddLegacySuggestedAmountTitle() {
        if (!Exists(PlatformsConstants.DonationFormState.LegacySuggestedAmount)) {
            return;
        }

        var designer = ForExisting(PlatformsConstants.DonationFormState.LegacySuggestedAmount);

        designer.Group(Groups.General).TextBox(TitleAlias).DataType(DataTypeNames.SuggestedAmountTitle);

        designer.Save();
    }

    private void AddSuggestedAmountTitle() {
        var designer = ForExistingElement<DonationFormStateSuggestedAmountElement>();

        designer.Group(Groups.General).TextBox(x => x.Title).DataType(DataTypeNames.SuggestedAmountTitle);

        designer.Save();
    }
}
