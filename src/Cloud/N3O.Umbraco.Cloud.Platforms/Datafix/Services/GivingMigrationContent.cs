using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoSystem = Umbraco.Cms.Core.Constants.System;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public static class GivingMigrationContent {
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

    // Recycle bin content is returned by the descendant walk but is not live, so it is filtered out everywhere.
    public static IEnumerable<IContent> GetAllContent(IContentService contentService) {
        long page = 0;
        long total;

        do {
            foreach (var content in contentService.GetPagedDescendants(UmbracoSystem.Root, page, PageSize, out total)) {
                if (!content.Trashed) {
                    yield return content;
                }
            }

            page++;
        } while (page * PageSize < total);
    }

    public static IReadOnlyList<IContent> GetDescendants(IContentService contentService, int parentId) {
        var descendants = new List<IContent>();
        long page = 0;
        long total;

        do {
            descendants.AddRange(contentService.GetPagedDescendants(parentId, page, PageSize, out total)
                                               .Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return descendants;
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
