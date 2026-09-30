using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Search.Typesense;

public static class TypesenseConverterRegistry {
    private static readonly Lazy<IReadOnlyList<ITypesenseConverter>> All = new(LoadAll);

    public static ITypesenseConverter GetConverter(Type type) {
        var converters = All.Value.Where(x => x.CanConvert(type)).ToList();

        if (converters.Count > 1) {
            throw new Exception($"Multiple Typesense converters apply to {type.Name.Quote()}: " +
                                converters.Select(x => x.GetType().Name).ToCsv());
        } else {
            return converters.SingleOrDefault();
        }
    }

    public static bool HasConverterFor(Type type) {
        return All.Value.Any(x => x.CanConvert(type));
    }

    private static IReadOnlyList<ITypesenseConverter> LoadAll() {
        return OurAssemblies.GetTypes(x => x.IsConcreteClass() &&
                                           !x.IsGenericTypeDefinition &&
                                           x.ImplementsInterface<ITypesenseConverter>() &&
                                           x.HasParameterlessConstructor())
                            .Select(x => (ITypesenseConverter) Activator.CreateInstance(x))
                            .ToList();
    }
}
