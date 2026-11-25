using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public class TagValuesAccessor : ITagValuesAccessor {
    private readonly ICdnClient _cdnClient;

    public TagValuesAccessor(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    public PublishedTagValues Get() {
        return _cdnClient.DownloadSubscriptionContentAsync<PublishedTagValues>(SubscriptionFiles.TagValues,
                                                                               JsonSerializers.JsonProvider)
                         .GetAwaiter()
                         .GetResult();
    }
}
