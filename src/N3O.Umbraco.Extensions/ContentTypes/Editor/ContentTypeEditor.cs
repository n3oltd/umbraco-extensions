using Humanizer;
using N3O.Umbraco.Content;
using N3O.Umbraco.Exceptions;
using System;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.ContentTypes;

public class ContentTypeEditor : IContentTypeEditor {
    private readonly IServiceProvider _serviceProvider;
    private readonly IContentTypeService _contentTypeService;
    private readonly IShortStringHelper _shortStringHelper;

    public ContentTypeEditor(IServiceProvider serviceProvider,
                             IContentTypeService contentTypeService,
                             IShortStringHelper shortStringHelper) {
        _serviceProvider = serviceProvider;
        _contentTypeService = contentTypeService;
        _shortStringHelper = shortStringHelper;
    }

    public IContentTypeDesigner ForExisting(string alias) {
        var contentType = _contentTypeService.Get(alias);

        if (contentType == null) {
            throw new ResourceNotFoundException(nameof(alias), alias);
        }

        if (contentType.IsElement) {
            return new ElementTypeDesigner(_serviceProvider,
                                           _contentTypeService,
                                           _shortStringHelper,
                                           contentType.Name,
                                           alias);
        } else {
            return new DocumentTypeDesigner(_serviceProvider,
                                            _contentTypeService,
                                            _shortStringHelper,
                                            contentType.Name,
                                            alias);
        }
    }

    public IDocumentTypeDesigner NewDocument(string name, string alias) {
        return new DocumentTypeDesigner(_serviceProvider, _contentTypeService, _shortStringHelper, name, alias);
    }

    public IDocumentTypeDesigner<T> NewDocument<T>() where T : IUmbracoContent {
        var alias = AliasHelper.ContentTypeAlias(typeof(T));

        return new DocumentTypeDesigner<T>(_serviceProvider,
                                           _contentTypeService,
                                           _shortStringHelper,
                                           alias.Titleize(),
                                           alias);
    }

    public IElementTypeDesigner NewElement(string name, string alias) {
        return new ElementTypeDesigner(_serviceProvider, _contentTypeService, _shortStringHelper, name, alias);
    }

    public IElementTypeDesigner<T> NewElement<T>() where T : IUmbracoElement {
        var alias = AliasHelper.ContentTypeAlias(typeof(T));

        return new ElementTypeDesigner<T>(_serviceProvider,
                                          _contentTypeService,
                                          _shortStringHelper,
                                          alias.Titleize(),
                                          alias);
    }
}
