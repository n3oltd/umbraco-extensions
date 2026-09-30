using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Reflection;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseJsonContractResolver : JsonContractResolver {
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
        var jsonProperty = base.CreateProperty(member, memberSerialization);

        var typesenseFieldName = TypesenseField.ToSearchFieldName(member);

        if (typesenseFieldName.HasValue()) {
            jsonProperty.PropertyName = typesenseFieldName;
        }

        var converter = TypesenseConverterRegistry.GetConverter(jsonProperty.PropertyType);

        if (converter != null) {
            var valueProvider = jsonProperty.ValueProvider;

            jsonProperty.Converter = TypesenseDispatchJsonConverter.Instance;
            jsonProperty.ShouldSerialize = instance => {
                return converter.ToTypesenseValue(valueProvider?.GetValue(instance)) != null;
            };
        } else if (typeof(IEnumerable).IsAssignableFrom(jsonProperty.PropertyType) &&
                   ResolveContract(jsonProperty.PropertyType) is JsonArrayContract arrayContract &&
                   arrayContract.CollectionItemType != null &&
                   TypesenseConverterRegistry.GetConverter(arrayContract.CollectionItemType) != null) {
            jsonProperty.ItemConverter = TypesenseDispatchJsonConverter.Instance;
        }

        return jsonProperty;
    }
}
