using N3O.Umbraco.DataTypes;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoDataTypes = Umbraco.Cms.Core.Constants.DataTypes;

namespace N3O.Umbraco.ContentTypes;

public class CheckBoxListPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<CheckBoxListPropertyTypeBuilder, CheckBoxListDataTypeDesigner> {
    public CheckBoxListPropertyTypeBuilder(IDataTypeService dataTypeService,
                                           CheckBoxListDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        if (HasConfiguration) {
            return BuildInlineDataType(context);
        } else {
            var key = Guid.Parse(UmbracoDataTypes.Guids.CheckboxList);

            return DataTypeService.GetAsync(key).GetAwaiter().GetResult();
        }
    }
}
