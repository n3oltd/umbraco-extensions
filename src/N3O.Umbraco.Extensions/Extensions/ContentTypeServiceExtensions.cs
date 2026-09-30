using System;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Extensions;

public static class ContentTypeServiceExtensions {
    public static IContentType GetOrThrow(this IContentTypeService contentTypeService, string contentTypeAlias) {
        var contentType = contentTypeService.Get(contentTypeAlias);

        if (contentType == null) {
            throw new Exception($"No content type found with alias {contentTypeAlias.Quote()}");
        }

        return contentType;
    }

    public static bool HasComposition(this IContentTypeService contentTypeService,
                                      string contentTypeAlias,
                                      string compositionAlias) {
        var contentType = contentTypeService.Get(contentTypeAlias);

        if (contentType == null) {
            return false;
        }

        return contentType.ContentTypeComposition.Any(x => x.Alias.EqualsInvariant(compositionAlias));
    }
}
