using System;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.ContentTypes;

public class ElementTypeDesigner : ContentTypeDesigner, IElementTypeDesigner {
    public ElementTypeDesigner(IServiceProvider serviceProvider,
                               IContentTypeService contentTypeService,
                               IShortStringHelper shortStringHelper,
                               string name,
                               string alias)
        : base(serviceProvider, contentTypeService, shortStringHelper, name, alias, true) { }

    public void AllowInLibrary() {
        // Umbraco has no library concept
    }
}

public class ElementTypeDesigner<T> : ElementTypeDesigner, IElementTypeDesigner<T> {
    public ElementTypeDesigner(IServiceProvider serviceProvider,
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
