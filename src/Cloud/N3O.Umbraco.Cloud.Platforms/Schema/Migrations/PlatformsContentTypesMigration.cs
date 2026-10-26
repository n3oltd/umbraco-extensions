using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using Umbraco.Cms.Infrastructure.Migrations;

namespace N3O.Umbraco.Cloud.Platforms;

public abstract class PlatformsContentTypesMigration : MigrationBase {
    private readonly IContentTypeEditor _contentTypeEditor;

    protected PlatformsContentTypesMigration(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context) {
        _contentTypeEditor = contentTypeEditor;
    }

    protected IDocumentTypeDesigner<T> ForExisting<T>() where T : IUmbracoContent {
        var alias = AliasHelper<T>.ContentTypeAlias();
        var existing = _contentTypeEditor.Find(alias) ?? throw new ContentTypeNotFoundException(alias);
        var designer = _contentTypeEditor.NewDocument<T>();

        designer.WithId(existing.Key);

        return designer;
    }

    protected IElementTypeDesigner<T> ForExistingElement<T>() where T : IUmbracoElement {
        var alias = AliasHelper<T>.ContentTypeAlias();
        var existing = _contentTypeEditor.Find(alias) ?? throw new ContentTypeNotFoundException(alias);
        var designer = _contentTypeEditor.NewElement<T>();

        designer.WithId(existing.Key);

        return designer;
    }
}
