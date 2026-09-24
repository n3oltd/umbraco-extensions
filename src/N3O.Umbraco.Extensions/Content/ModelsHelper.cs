using Humanizer;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Content;

public class ModelsHelper {
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.InvariantCultureIgnoreCase);

    public static Type GetOrCreateModelsBuilderType(string modelsNamespace, string contentType) {
        var cacheKey = CacheKey.Generate<ModelsHelper>(modelsNamespace, contentType);
        
        return TypeCache.GetOrAdd(cacheKey, () => {
            var typeName = contentType.Pascalize();

            var type = OurAssemblies.GetTypes(t => t.Name == typeName)
                                    .FirstOrDefault(x => x.IsInterface ||
                                                         (x.IsConcreteClass() && ((x.IsSubclassOfType(typeof(PublishedContentModel)) || x.IsSubclassOfType(typeof(PublishedElementModel))))));

            if (type == null) {
                var fullTypeName = $"{modelsNamespace}.{typeName}";
                var assemblyName = new AssemblyName($"assembly_{Guid.NewGuid()}");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule($"module_{Guid.NewGuid()}");

                var typeAttributes = TypeAttributes.AnsiClass |
                                     TypeAttributes.AutoClass |
                                     TypeAttributes.Class |
                                     TypeAttributes.ExplicitLayout |
                                     TypeAttributes.Public;

                var typeBuilder = moduleBuilder.DefineType(fullTypeName, typeAttributes, null);

                type = typeBuilder.CreateType();
            }

            return type;
        });
    }
}
