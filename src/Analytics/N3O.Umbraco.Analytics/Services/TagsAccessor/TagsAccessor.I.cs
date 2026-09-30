using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.Analytics;

public interface ITagsAccessor {
    JObject GetTags();
}
