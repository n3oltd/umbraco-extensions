using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Search.Typesense;

public class ContentIndexer : IContentIndexer {
    private readonly IReadOnlyList<ISearchIndexer> _searchIndexers;

    public ContentIndexer(IEnumerable<ISearchIndexer> searchIndexers) {
        _searchIndexers = searchIndexers.ApplyAttributeOrdering();
    }

    public async Task IndexAsync(IPublishedContent content) {
        var searchIndexers = _searchIndexers.Where(x => x.CanIndex(content)).OrEmpty().ToList();

        foreach (var searchIndexer in searchIndexers) {
            if (content.ContentType.VariesByCulture()) {
                await searchIndexer.DeleteAsync(content.Key);

                foreach (var publishedContentCulture in content.Cultures) {
                    await searchIndexer.IndexAsync(content, publishedContentCulture.Value.Culture);
                }
            } else {
                await searchIndexer.IndexAsync(content);
            }
        }
    }
}
