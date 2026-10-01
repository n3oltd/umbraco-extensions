using N3O.Umbraco.Data.Converters;
using N3O.Umbraco.Data.Filters;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Data.Extensions;

public static class UmbracoPropertyInfoExtensions {
    public static bool CanInclude(this UmbracoPropertyInfo propertyInfo,
                                  IEnumerable<IExportPropertyFilter> propertyFilters) {
        var filters = propertyFilters.OrEmpty().Where(x => x.IsFilter(propertyInfo)).ToList();
        
        foreach (var filter in filters) {
            if (!filter.CanExport(propertyInfo)) {
                return false;
            }
        }

        return true;
    }
    
    public static bool CanInclude(this UmbracoPropertyInfo propertyInfo,
                                  IEnumerable<IImportPropertyFilter> propertyFilters) {
        var filters = propertyFilters.OrEmpty().Where(x => x.IsFilter(propertyInfo)).ToList();
        
        foreach (var filter in filters) {
            if (!filter.CanImport(propertyInfo)) {
                return false;
            }
        }

        return true;
    }
    
    public static IReadOnlyList<Column> GetColumns(this UmbracoPropertyInfo propertyInfo,
                                                   IEnumerable<IPropertyConverter> converters,
                                                   string columnTitlePrefix = null) {
        var converter = GetPropertyConverter(propertyInfo, converters);

        return converter.GetColumns(converters, propertyInfo, columnTitlePrefix ?? "");
    }
    
    public static string GetColumnTitle(this UmbracoPropertyInfo propertyInfo, string columnTitlePrefix = null) {
        var name = columnTitlePrefix ?? "";

        if (propertyInfo.Group.HasValue() && propertyInfo.ContentType.PropertyGroups.Count > 1) {
            name += $"{propertyInfo.Group.Name}: ";
        }
        
        name += propertyInfo.Type.Name;

        return name;
    }

    public static IPropertyConverter GetPropertyConverter(this UmbracoPropertyInfo propertyInfo,
                                                          IEnumerable<IPropertyConverter> converters) {
        var matches = converters.Where(x => x.IsConverter(propertyInfo)).ToList();

        if (matches.None()) {
            throw new Exception($"No property converter found for {propertyInfo.DataType.EditorAlias.Quote()}");
        } else if (matches.Count > 1) {
            throw new Exception($"More than one property converter found for {propertyInfo.DataType.EditorAlias.Quote()}");
        } else {
            return matches.Single();
        }
    }
    
    public static bool HasPropertyConverter(this UmbracoPropertyInfo propertyInfo,
                                            IEnumerable<IPropertyConverter> converters) {
        try {
            GetPropertyConverter(propertyInfo, converters);

            return true;
        } catch {
            return false;
        }
    }
    
    public static bool HasTimeFormat(this UmbracoPropertyInfo propertyInfo) {
        var format = propertyInfo.DataType
                                 .ConfigurationData
                                 .FirstOrDefault(x => x.Key.EqualsInvariant("format"))
                                 .Value as string;

        // The date picker shows no time when the format is absent or has no hour component.
        return format?.Contains("h", StringComparison.InvariantCultureIgnoreCase) == true;
    }
}
