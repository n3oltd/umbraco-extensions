using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Builders;
using N3O.Umbraco.Data.Extensions;
using N3O.Umbraco.Data.Lookups;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Data.Parsing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;
using OurDataTypes = N3O.Umbraco.Data.Lookups.DataTypes;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.Data.Converters;

public class BlockListPropertyConverter : IPropertyConverter {
    private readonly IColumnRangeBuilder _columnRangeBuilder;
    private readonly Dictionary<string, IColumnRange> _columnRanges = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly string _orderColumnTitle;

    public BlockListPropertyConverter(IColumnRangeBuilder columnRangeBuilder, IFormatter formatter) {
        _columnRangeBuilder = columnRangeBuilder;
        
        _orderColumnTitle = formatter.Text.Format<DataStrings>(s => s.OrderColumnTitle);
    }
    
    public bool IsConverter(UmbracoPropertyInfo propertyInfo) {
        return propertyInfo.Type.PropertyEditorAlias.EqualsInvariant(UmbracoPropertyEditors.Aliases.BlockList);
    }

    public void Export(IUntypedTableBuilder tableBuilder,
                       IEnumerable<IPropertyConverter> converters,
                       int columnOrder,
                       string columnTitlePrefix,
                       IContentProperty contentProperty,
                       UmbracoPropertyInfo propertyInfo) {
        var blockListConfiguration = propertyInfo.DataType.ConfigurationAs<BlockListConfiguration>();

        foreach (var (elementsProperties, index) in ((ElementsProperty) contentProperty).OrEmpty(x => x.Value).SelectWithIndex()) {
            var elementInfo = propertyInfo.Elements
                                          .Single(x => x.ContentType
                                                        .Alias
                                                        .EqualsInvariant(elementsProperties.ContentTypeAlias));

            var elementColumnTitlePrefix = GetColumnTitlePrefix(propertyInfo,
                                                                elementInfo,
                                                                index + 1,
                                                                columnTitlePrefix);
            
            foreach (var elementPropertyInfo in elementInfo.Properties) {
                var converter = elementPropertyInfo.GetPropertyConverter(converters);
                var elementsProperty = elementsProperties.GetPropertyByAlias(elementPropertyInfo.Type.Alias);

                converter.Export(tableBuilder,
                                 converters,
                                 columnOrder,
                                 elementColumnTitlePrefix,
                                 elementsProperty,
                                 elementPropertyInfo);
                
                columnOrder += 100;
            }

            if (!blockListConfiguration.Blocks.IsSingle()) {
                var orderColumnRange = GetOrAddColumnRange<int?>(OurDataTypes.Integer,
                                                                 GetOrderColumnTitle(elementColumnTitlePrefix));
                
                tableBuilder.AddValue(orderColumnRange, index + 1);
            }
        }
    }

    public IReadOnlyList<Column> GetColumns(IEnumerable<IPropertyConverter> converters,
                                            UmbracoPropertyInfo propertyInfo,
                                            string columnTitlePrefix) {
        var columns = new List<Column>();
        var maxValues = GetMaxValues(propertyInfo);
        var blockListConfiguration = propertyInfo.DataType.ConfigurationAs<BlockListConfiguration>();

        foreach (var element in propertyInfo.Elements) {
            for (var i = 1; i <= maxValues; i++) {
                var blockListColumnTitlePrefix = GetColumnTitlePrefix(propertyInfo,
                                                                      element,
                                                                      i,
                                                                      columnTitlePrefix);

                foreach (var elementPropertyInfo in element.Properties) {
                    columns.AddRange(elementPropertyInfo.GetColumns(converters, blockListColumnTitlePrefix));
                }
                
                if (!blockListConfiguration.Blocks.IsSingle()) {
                    var orderColumnRange = GetOrAddColumnRange<int?>(OurDataTypes.Integer,
                                                                     GetOrderColumnTitle(blockListColumnTitlePrefix));

                    orderColumnRange.AddValues(0, null);
                    
                    columns.AddRange(orderColumnRange.GetColumns());
                }
            }
        }

        return columns;
    }

    public void Import(IContentBuilder contentBuilder,
                       IEnumerable<IPropertyConverter> converters,
                       IParser parser,
                       ErrorLog errorLog,
                       string columnTitlePrefix,
                       UmbracoPropertyInfo propertyInfo,
                       IEnumerable<ImportField> fields) {
        var maxValues = GetMaxValues(propertyInfo);
        var blockListPropertyBuilder = contentBuilder.BlockList(propertyInfo.Type.Alias);
        var blockListConfiguration = propertyInfo.DataType.ConfigurationAs<BlockListConfiguration>();

        foreach (var element in propertyInfo.Elements) {
            for (var i = 1; i <= maxValues; i++) {
                var nestedColumnTitlePrefix = GetColumnTitlePrefix(propertyInfo,
                                                                   element,
                                                                   i,
                                                                   columnTitlePrefix);

                if (!HasData(nestedColumnTitlePrefix, fields)) {
                    break;
                }

                int? order = null;
                
                if (!blockListConfiguration.Blocks.IsSingle()) {
                    var orderColumnTitle = GetOrderColumnTitle(nestedColumnTitlePrefix);
                    
                    var orderField = fields.Single(x => x.Name.EqualsInvariant(orderColumnTitle));
                    order = orderField.Value.TryParseAs<int>();
                }

                IContentBuilder blockListBuilder = null;
                
                foreach (var elementPropertyInfo in element.Properties) {
                    var nestedColumnTitle = elementPropertyInfo.GetColumnTitle(nestedColumnTitlePrefix);

                    var nestedFields = fields.Where(f => f.Name.InvariantStartsWith(nestedColumnTitle)).ToList();

                    var converter = elementPropertyInfo.GetPropertyConverter(converters);

                    blockListBuilder ??= blockListPropertyBuilder.Add(element.ContentType.Alias, order: order);

                    converter.Import(blockListBuilder,
                                     converters,
                                     parser,
                                     errorLog,
                                     nestedColumnTitle,
                                     elementPropertyInfo,
                                     nestedFields);
                }
            }
        }
    }

    private int GetMaxValues(UmbracoPropertyInfo propertyInfo) {
        var configuration = propertyInfo.DataType.ConfigurationAs<BlockListConfiguration>();

        if (configuration.ValidationLimit.Max == null || configuration.ValidationLimit.Max == 0) {
            return DataConstants.Limits.Columns.MaxValues;
        } else {
            return configuration.ValidationLimit.Max.GetValueOrThrow();
        }
    }
    
    private IColumnRange GetOrAddColumnRange<T>(DataType dataType, string title) {
        return _columnRanges.GetOrAdd(title,
                                      () => _columnRangeBuilder.OfType<T>(dataType)
                                                               .Title(title)
                                                               .PreserveColumnOrder()
                                                               .Build());
    }
    
    private string GetColumnTitlePrefix(UmbracoPropertyInfo propertyInfo,
                                        ElementInfo elementInfo,
                                        int i,
                                        string columnTitlePrefix) {
        var titlePrefix = propertyInfo.GetColumnTitle(columnTitlePrefix);

        if (i == 1) {
            titlePrefix += $" {DataConstants.Separator} ";
        } else {
            titlePrefix += $" {i} {DataConstants.Separator} ";
        }
        
        if (!propertyInfo.Elements.IsSingle()) {
            titlePrefix += $" {elementInfo.ContentType.Name}: ";
        }

        return titlePrefix;
    }

    private string GetOrderColumnTitle(string columnTitlePrefix) {
        return $"{columnTitlePrefix} {_orderColumnTitle}";
    }
    
    private bool HasData(string nestedColumnTitlePrefix, IEnumerable<ImportField> fields) {
        return fields.Where(x => x.Name.StartsWith(nestedColumnTitlePrefix,
                                                   StringComparison.InvariantCultureIgnoreCase))
                     .Any(x => x.Value.HasValue());
    }
}
