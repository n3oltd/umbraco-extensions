using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Json;
using N3O.Umbraco.Utilities;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using System;
using System.Linq;
using System.Reflection;

namespace N3O.Umbraco.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddOpenApiDocument(this IServiceCollection services, string name) {
        if (OpenApi.IsEnabled()) {
            services.AddOpenApiDocument((opt, serviceProvider) => {
                opt.Title = name;
                opt.DocumentName = name;

                if (IsWrittenByOurJson(name)) {
                    UseOurJsonSchema(opt, serviceProvider);
                }

                opt.SchemaSettings.FlattenInheritanceHierarchy  = true;

                AddSchemaProcessors(opt);
                AddOperationProcessors(opt);
                AddOperationFilters(opt, name);
            });
        }

        return services;
    }

    private static bool IsWrittenByOurJson(string name) {
        var controllerTypes = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                          name.EqualsInvariant(GetApiName(t)))
                                           .ToList();

        return controllerTypes.Any() && controllerTypes.All(t => t.GetCustomAttribute<OurJsonFilter>() != null);
    }

    private static string GetApiName(Type type) {
        return type.GetCustomAttribute<ApiDocumentAttribute>()?.ApiName;
    }

    private static void UseOurJsonSchema(AspNetCoreOpenApiDocumentGeneratorSettings opt,
                                         IServiceProvider serviceProvider) {
        var jsonProvider = serviceProvider.GetRequiredService<IJsonProvider>();

        var schemaSettings = new NewtonsoftJsonSchemaGeneratorSettings();
        schemaSettings.SerializerSettings = jsonProvider.GetSettings();
        schemaSettings.SchemaType = SchemaType.OpenApi3;

        opt.SchemaSettings = schemaSettings;
    }
    
    private static void AddSchemaProcessors(AspNetCoreOpenApiDocumentGeneratorSettings opt) {
        var schemaProcessors = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                           t.ImplementsInterface<ISchemaProcessor>() &&
                                                           t.HasParameterlessConstructor())
                                            .Select(t => (ISchemaProcessor) Activator.CreateInstance(t))
                                            .ToList();
        
        schemaProcessors.Do(opt.SchemaSettings.SchemaProcessors.Add);
    }
    
    private static void AddOperationProcessors(AspNetCoreOpenApiDocumentGeneratorSettings opt) {
        var operationProcessors = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                           t.ImplementsInterface<IOperationProcessor>() &&
                                                           t.HasParameterlessConstructor())
                                            .Select(t => (IOperationProcessor) Activator.CreateInstance(t))
                                            .ToList();
        
        operationProcessors.Do(opt.OperationProcessors.Add);
    }

    private static void AddOperationFilters(AspNetCoreOpenApiDocumentGeneratorSettings opt, string name) {
        opt.AddOperationFilter(ctx => {
            if (name.EqualsInvariant(ctx.ControllerType.GetCustomAttribute<ApiDocumentAttribute>()?.ApiName)) {
                return true;
            } else {
                return false;
            }
        });
    }
}
