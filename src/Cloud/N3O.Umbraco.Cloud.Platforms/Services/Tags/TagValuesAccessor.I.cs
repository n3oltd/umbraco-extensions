using N3O.Umbraco.Cloud.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ITagValuesAccessor {
    PublishedContentResult<PublishedTagValues> Get();
}
