using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV3 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV3(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        if (Exists(PlatformsConstants.DonationFormState.LegacySuggestedAmount)) {
            AddSuggestedAmountTitle(PlatformsConstants.DonationFormState.LegacySuggestedAmount);
        }

        AddSuggestedAmountTitle(PlatformsConstants.DonationFormState.SuggestedAmount);
    }

    private void AddSuggestedAmountTitle(string alias) {
        var designer = ForExisting(alias);
        var titleAlias = AliasHelper<DonationFormStateSuggestedAmountElement>.PropertyAlias(x => x.Title);

        designer.Group(Groups.General).TextBox(titleAlias).DataType(DataTypeNames.SuggestedAmountTitle);

        designer.Save();
    }
}
