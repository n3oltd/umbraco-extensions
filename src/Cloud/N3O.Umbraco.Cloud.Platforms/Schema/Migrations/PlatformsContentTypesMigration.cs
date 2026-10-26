using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using System;
using Umbraco.Cms.Infrastructure.Migrations;

namespace N3O.Umbraco.Cloud.Platforms;

public abstract class PlatformsContentTypesMigration : MigrationBase {
    private readonly IContentTypeEditor _contentTypeEditor;

    protected PlatformsContentTypesMigration(IMigrationContext context, IContentTypeEditor contentTypeEditor)
        : base(context) {
        _contentTypeEditor = contentTypeEditor;
    }

    protected bool Exists(string alias) {
        return _contentTypeEditor.Find(alias) != null;
    }

    protected IContentTypeDesigner ForExisting(string alias) {
        var designer = _contentTypeEditor.ForExisting(alias);

        designer.WithId(GetExistingKey(alias));

        return designer;
    }

    protected IDocumentTypeDesigner<T> ForExisting<T>() where T : IUmbracoContent {
        var designer = _contentTypeEditor.NewDocument<T>();

        designer.WithId(GetExistingKey(AliasHelper<T>.ContentTypeAlias()));

        return designer;
    }

    protected IElementTypeDesigner<T> ForExistingElement<T>() where T : IUmbracoElement {
        var designer = _contentTypeEditor.NewElement<T>();

        designer.WithId(GetExistingKey(AliasHelper<T>.ContentTypeAlias()));

        return designer;
    }

    private Guid GetExistingKey(string alias) {
        var existing = _contentTypeEditor.Find(alias) ?? throw new ContentTypeNotFoundException(alias);

        return existing.Key;
    }
}
