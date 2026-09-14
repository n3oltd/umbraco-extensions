using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.ContentTypes;

public interface IPropertyTypeBuilder {
    void Apply(IPropertyType propertyType, PropertyTypeContext context);
    IDataType ResolveDataType(PropertyTypeContext context);
}
