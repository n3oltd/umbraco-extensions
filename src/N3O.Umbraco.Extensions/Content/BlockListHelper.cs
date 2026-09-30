using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

// The editor migration rewrites Nested Content properties to Block Lists, whose items wrap their element rather
// than being one. Single block mode answers one item instead of a list.
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

    public static TProperty GetSingleAs<TProperty>(IPublishedProperty property,
                                                   object propertyValue,
                                                   IPublishedContent parent) {
        var element = GetSingleElement(property, propertyValue);

        if (element == null) {
            return default;
        }

        if (element is not TProperty && !typeof(TProperty).ImplementsInterface<IUmbracoElement>()) {
            return default;
        }

        return element.As<TProperty>(parent);
    }

    // Nested Content answered a single element only when a property allowed exactly one item, and the editor
    // migration carries those limits onto the Block List.
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
