using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoDataTypes = Umbraco.Cms.Core.Constants.DataTypes;

namespace N3O.Umbraco.ContentTypes;

public class DatePropertyTypeBuilder : PropertyTypeBuilder<DatePropertyTypeBuilder> {
    public DatePropertyTypeBuilder(IDataTypeService dataTypeService) : base(dataTypeService) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        var key = Guid.Parse(UmbracoDataTypes.Guids.DatePicker);

        return DataTypeService.GetAsync(key).GetAwaiter().GetResult();
    }
}
