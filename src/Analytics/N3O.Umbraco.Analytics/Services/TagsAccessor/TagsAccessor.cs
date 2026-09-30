using N3O.Umbraco.Analytics.Context;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.Analytics;

public class TagsAccessor : ITagsAccessor {
    private readonly TagsCookie _tagsCookie;

    public TagsAccessor(TagsCookie tagsCookie) {
        _tagsCookie = tagsCookie;
    }

    public JObject GetTags() {
        return _tagsCookie.GetTags();
    }
}
