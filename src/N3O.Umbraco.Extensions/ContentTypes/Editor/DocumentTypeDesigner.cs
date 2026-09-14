using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.ContentTypes;

public class DocumentTypeDesigner : ContentTypeDesigner, IDocumentTypeDesigner {
    private readonly List<string> _allowedChildren = [];

    private bool _allowAtRoot;

    public DocumentTypeDesigner(IServiceProvider serviceProvider,
                                IContentTypeService contentTypeService,
                                IShortStringHelper shortStringHelper,
                                string name,
                                string alias)
        : base(serviceProvider, contentTypeService, shortStringHelper, name, alias, false) { }

    public void AllowAtRoot() {
        _allowAtRoot = true;
    }

    public void AllowChildren(params string[] contentTypeAliases) {
        _allowedChildren.AddRange(contentTypeAliases);
    }

    protected override void ApplyKind(IContentType contentType) {
        if (_allowAtRoot) {
            contentType.AllowedAsRoot = true;
        }

        if (!_allowedChildren.None()) {
            var allowed = contentType.AllowedContentTypes.OrEmpty().ToList();

            foreach (var alias in _allowedChildren) {
                if (allowed.All(x => x.Alias != alias)) {
                    var child = alias.EqualsInvariant(Alias) ? contentType : ResolveContentType(alias);

                    allowed.Add(new ContentTypeSort(child.Key, allowed.Count, child.Alias));
                }
            }

            contentType.AllowedContentTypes = allowed;
        }
    }
}

public class DocumentTypeDesigner<T> : DocumentTypeDesigner, IDocumentTypeDesigner<T> {
    public DocumentTypeDesigner(IServiceProvider serviceProvider,
                                IContentTypeService contentTypeService,
                                IShortStringHelper shortStringHelper,
                                string name,
                                string alias)
        : base(serviceProvider, contentTypeService, shortStringHelper, name, alias) { }

    public new IPropertyContainerBuilder<T> Group(string name) {
        var container = new PropertyContainerBuilder<T>(ServiceProvider, ShortStringHelper, name, false, null);

        RegisterContainer(container);

        return container;
    }

    public new IPropertyContainerBuilder<T> Tab(string name) {
        var container = new PropertyContainerBuilder<T>(ServiceProvider, ShortStringHelper, name, true, null);

        RegisterContainer(container);

        return container;
    }
}
