using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace N3O.Umbraco.ValueConverters;

// Umbraco's UploadPropertyConverter ends "source?.ToString() ?? string.Empty", so an upload property with
// no value answers "" rather than null. The retired N3O Uploader answered null, and the media migration
// converts every Uploader property to Umbraco.UploadField, so without this a template that asks
// "!= null" silently flips to the has-a-value branch on a property that has none.
public class EmptyAsNullUploadValueConverter : UploadPropertyConverter {
    public override object ConvertIntermediateToObject(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       PropertyCacheLevel cacheLevel,
                                                       object source,
                                                       bool preview) {
        var value = base.ConvertIntermediateToObject(owner, propertyType, cacheLevel, source, preview) as string;

        return value.HasValue() ? value : null;
    }
}
