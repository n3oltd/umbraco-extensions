using N3O.Umbraco.Cloud.Platforms.ContentTypes;
using N3O.Umbraco.ContentTypes;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class PropertyContainerBuilderExtensions {
    public static CampaignTagsPropertyTypeBuilder CampaignTags<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<CampaignTagsPropertyTypeBuilder, TProperty>(expression);
    }
}
