using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Community.Contentment.DataEditors;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DataListItemExtensions {
    public static IReadOnlyDictionary<string, string> ToTagsDictionary(this IEnumerable<DataListItem> dataListItems) {
        return dataListItems.OrEmpty().ToDictionary(x => x.Name, x => x.Value);
    }
}