using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Maps.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Maps.ContentTypes;

public class GoogleMapsPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<GoogleMapsPropertyTypeBuilder, GoogleMapsDataTypeDesigner> {
    public GoogleMapsPropertyTypeBuilder(IDataTypeService dataTypeService,
                                         GoogleMapsDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
