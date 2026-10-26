using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV3 : PlatformsContentTypesMigration {
    public PlatformsContentTypesMigrationV3(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context, contentTypeEditor) { }

    protected override void Migrate() {
        AddSuggestedAmountTitle();
    }

    private void AddSuggestedAmountTitle() {
        var designer = ForExistingElement<DonationFormStateSuggestedAmountElement>();

        designer.Group(Groups.General).TextBox(x => x.Title).DataType(Shared.TextBox);

        designer.Save();
    }
}
