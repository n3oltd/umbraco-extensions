using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Builders;
using N3O.Umbraco.Data.Extensions;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Data.Parsing;
using N3O.Umbraco.Extensions;
using NodaTime;
using System;
using System.Collections.Generic;
using OurDataTypes = N3O.Umbraco.Data.Lookups.DataTypes;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.Data.Converters;

public class DatePropertyConverter : PropertyConverter<LocalDate?> {
    public DatePropertyConverter(IColumnRangeBuilder columnRangeBuilder) : base(columnRangeBuilder) { }
    
    public override bool IsConverter(UmbracoPropertyInfo propertyInfo) {
        return propertyInfo.Type.PropertyEditorAlias.EqualsInvariant(UmbracoPropertyEditors.Aliases.DateTime) &&
               !propertyInfo.HasTimeFormat();
    }

    protected override IEnumerable<Cell<LocalDate?>> GetCells(IContentProperty contentProperty,
                                                                  UmbracoPropertyInfo propertyInfo) {
        return ExportValue<DateTime?>(contentProperty, x => OurDataTypes.Date.Cell(x?.ToLocalDate()));
    }

    public override void Import(IContentBuilder contentBuilder,
                                IEnumerable<IPropertyConverter> converters,
                                IParser parser,
                                ErrorLog errorLog,
                                string columnTitlePrefix,
                                UmbracoPropertyInfo propertyInfo,
                                IEnumerable<ImportField> fields) {
        Import(errorLog,
               propertyInfo,
               fields,
               s => parser.Date.Parse(s, OurDataTypes.Date.GetClrType()),
               (alias, value) => contentBuilder.DateTime(alias).SetDate(value));
    }
}
