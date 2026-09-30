using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.ContentTypes;

public interface IPropertyTypeBuilder {
    void Apply(IPropertyType propertyType, PropertyTypeContext context, bool isNew);
    IDataType ResolveDataType(PropertyTypeContext context);
}
