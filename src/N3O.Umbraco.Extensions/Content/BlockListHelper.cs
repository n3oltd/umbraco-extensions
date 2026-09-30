using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

public static class BlockListHelper {
    public static IEnumerable<IPublishedElement> GetElements(object propertyValue) {
        if (propertyValue is BlockListItem item) {
            return [item.Content];
        } else if (propertyValue is IEnumerable<BlockListItem> items) {
            return items.Select(x => x.Content);
        } else {
            return null;
        }
    }

    public static bool TryGetSingleAs<TProperty>(IPublishedProperty property,
                                                 object propertyValue,
                                                 IPublishedContent parent,
                                                 out TProperty value) {
        var element = GetSingleElement(property, propertyValue);

        if (element == null || (element is not TProperty && !typeof(TProperty).ImplementsInterface<IUmbracoElement>())) {
            value = default;

            return false;
        }

        value = element.As<TProperty>(parent);

        return true;
    }

    private static IPublishedElement GetSingleElement(IPublishedProperty property, object propertyValue) {
        if (propertyValue is BlockListItem item) {
            return item.Content;
        }

        if (propertyValue is not IEnumerable<BlockListItem> items) {
            return null;
        }

        var validationLimit = property.PropertyType.DataType.ConfigurationAs<BlockListConfiguration>()?.ValidationLimit;

        if (validationLimit?.Min != 1 || validationLimit.Max != 1) {
            return null;
        }

        return items.FirstOrDefault()?.Content;
    }
}
