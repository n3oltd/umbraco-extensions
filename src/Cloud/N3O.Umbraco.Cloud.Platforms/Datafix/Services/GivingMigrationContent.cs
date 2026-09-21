using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
internal static class GivingMigrationContent {
    private const int PageSize = 200;

    public static IReadOnlyList<IContent> GetAllOfAlias(IContentService contentService,
                                                        IContentTypeService contentTypeService,
                                                        string contentTypeAlias) {
        var contentType = contentTypeService.Get(contentTypeAlias);

        if (contentType == null) {
            return [];
        }

        return GetAllOfType(contentService, contentType.Id);
    }

    public static IReadOnlyList<IContent> GetAllOfType(IContentService contentService, int contentTypeId) {
        var items = new List<IContent>();
        long page = 0;
        long total;

        do {
            items.AddRange(contentService.GetPagedOfType(contentTypeId, page, PageSize, out total, null));

            page++;
        } while (page * PageSize < total);

        return items;
    }

    public static IReadOnlyList<IContent> GetChildren(IContentService contentService, int parentId) {
        var children = new List<IContent>();
        long page = 0;
        long total;

        do {
            children.AddRange(contentService.GetPagedChildren(parentId, page, PageSize, out total)
                                            .Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return children;
    }

    public static IReadOnlyList<int> GetOfferingContentTypeIds(IContentTypeService contentTypeService) {
        var composition = contentTypeService.Get(PlatformsConstants.Offerings.CompositionAlias);

        if (composition == null) {
            return [];
        }

        return contentTypeService.GetComposedOf(composition.Id).Select(x => x.Id).ToList();
    }
}
