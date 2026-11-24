using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public class TagValuesAccessor : ITagValuesAccessor {
    private readonly ICdnClient _cdnClient;

    public TagValuesAccessor(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    public PublishedContentResult<PublishedTagValues> Get() {
        return _cdnClient.DownloadPublishedContentAsync<PublishedTagValues>(PublishedFileKinds.Subscription,
                                                                            SubscriptionFiles.TagValues.Filename,
                                                                            JsonSerializers.JsonProvider)
                         .GetAwaiter()
                         .GetResult();
    }
}
