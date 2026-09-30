using N3O.Umbraco.Analytics;

namespace N3O.Umbraco.Giving.Checkout.Entities;

public partial class Checkout {
    public void UpdateTags(ITagsAccessor tagsAccessor) {
        Tags = tagsAccessor.GetTags();
    }
}
