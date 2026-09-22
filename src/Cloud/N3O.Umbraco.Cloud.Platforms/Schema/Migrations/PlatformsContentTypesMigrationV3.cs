using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsContentTypesMigrationV3 : MigrationBase {
    private readonly IContentTypeEditor _contentTypeEditor;

    public PlatformsContentTypesMigrationV3(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context) {
        _contentTypeEditor = contentTypeEditor;
    }

    protected override void Migrate() {
        AddCrossSellAmount();
    }

    private void AddCrossSellAmount() {
        var designer = ForExisting<CrossSellContent>();

        designer.Group(Groups.General).Decimal(x => x.Amount).DataType(Shared.Money);

        designer.Save();
    }

    private IDocumentTypeDesigner<T> ForExisting<T>() where T : IUmbracoContent {
        var alias = AliasHelper<T>.ContentTypeAlias();
        var existing = _contentTypeEditor.Find(alias) ?? throw new ContentTypeNotFoundException(alias);
        var designer = _contentTypeEditor.NewDocument<T>();

        designer.WithId(existing.Key);

        return designer;
    }
}
