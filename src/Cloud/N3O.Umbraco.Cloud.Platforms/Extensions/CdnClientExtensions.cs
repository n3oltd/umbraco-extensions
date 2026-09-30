using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using System.Threading;
using System.Threading.Tasks;
using PublishedFileInfo = N3O.Umbraco.Cloud.Models.PublishedFileInfo;
using PlatformsPage = N3O.Umbraco.Cloud.Platforms.Models.PlatformsPage;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class CdnClientExtensions {
    public static async Task<GetPageResult> DownloadPlatformsPageAsync(this ICdnClient cdnClient,
                                                                       IJsonProvider jsonProvider,
                                                                       PublishedFileKind kind,
                                                                       SpecialContent parent,
                                                                       string path,
                                                                       CancellationToken cancellationToken = default) {
        var pagePath = $"{kind.Id}/{path.Trim('/')}/index.json";
        
        var publishedContentResult = await cdnClient.DownloadPublishedContentAsync(pagePath, cancellationToken);

        if (publishedContentResult.Error) {
            return GetPageResult.ForError();
        } else if (publishedContentResult.NotFound) {
            return null;
        } else {
            if (publishedContentResult.Kind == PublishedFileKinds.PageRedirect) {
                var publishedPageRedirect = jsonProvider.DeserializeDynamicTo<PublishedPageRedirect>(publishedContentResult.Content);

                return GetPageResult.ForRedirect(publishedPageRedirect.Url.AbsoluteUri,
                                                 publishedPageRedirect.Temporary.GetValueOrDefault());
            }
            
            var publishedPlatformsPage = jsonProvider.DeserializeDynamicTo<PublishedPlatformsPage>(publishedContentResult.Content);

            var additionalModels = await publishedPlatformsPage.OrEmpty(x => x.MergeModels)
                                                               .SelectListAsync(x => FetchMergeModelAsync(cdnClient, x));

            if (additionalModels.HasAny(x => x.Error)) {
                return GetPageResult.ForError();
            }

            var platformsPage = new PlatformsPage(publishedContentResult.Id.GetValueOrThrow(),
                                                  publishedContentResult.Kind,
                                                  parent,
                                                  publishedContentResult.Path,
                                                  publishedPlatformsPage.Title,
                                                  publishedPlatformsPage.Url,
                                                  publishedContentResult.Content,
                                                  publishedPlatformsPage.MetaTags,
                                                  additionalModels);

            return GetPageResult.ForPage(platformsPage);
        }
    }
     
    private static Task<PublishedContentResult> FetchMergeModelAsync(ICdnClient cdnClient,
                                                                     PublishedFileInfo publishedModel) {
        return cdnClient.DownloadPublishedContentAsync(publishedModel.Path);
    }
}