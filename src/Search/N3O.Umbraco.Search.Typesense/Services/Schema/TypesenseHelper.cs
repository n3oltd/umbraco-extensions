using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Attributes;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public static class TypesenseHelper {
    private static readonly ConcurrentDictionary<Type, CollectionInfo> CollectionsMap = new();

    public static IReadOnlyList<CollectionInfo> GetAllCollections() => CollectionsMap.Values.ToList();
    
    public static CollectionInfo GetCollection<TDocument>() => CollectionsMap.GetValueOrDefault(typeof(TDocument));
    
    public static void RegisterCollectionFor<TDocument>(params string[] contentTypeAliases) {
        var attribute = typeof(TDocument).GetCustomAttribute<CollectionAttribute>();

        if (attribute == null) {
            throw new Exception($"Type {typeof(TDocument).GetFriendlyName()} is missing a required {nameof(CollectionAttribute)}");
        }

        var fields = GetFields<TDocument>();
        var collectionInfo = new CollectionInfo(new CollectionName(attribute.Name),
                                                attribute.Version,
                                                contentTypeAliases,
                                                fields);

        CollectionsMap[typeof(TDocument)] = collectionInfo;
    }

    private static IReadOnlyList<Field> GetFields<TDocument>() {
        var fields = new List<Field>();

        AddFields<TDocument>(fields);
        AddIndexFields<TDocument>(fields);

        ValidateDeclaredFieldTypes<TDocument>();
        ValidateNoDuplicates<TDocument>(fields);

        return fields;
    }

    private static void AddFields<TDocument>(List<Field> fields) {
        var documentType = typeof(TDocument);

        foreach (var property in documentType.GetProperties()) {
            var attribute = property.GetCustomAttribute<FieldAttribute>();

            if (attribute == null) {
                continue;
            } else if (NestedFieldExpander.ShouldExpand(attribute)) {
                fields.AddRange(NestedFieldExpander.Expand(documentType, property, attribute));
            } else {
                var field = new Field(attribute.Name,
                                      attribute.Type,
                                      attribute.Facet,
                                      !attribute.Required,
                                      attribute.Index,
                                      attribute.Sort,
                                      attribute.Infix,
                                      attribute.Locale,
                                      attribute.NumberOfDimensions);

                fields.Add(field);
            }
        }
    }

    private static void AddIndexFields<TDocument>(List<Field> fields) {
        var attributes = typeof(TDocument).GetProperties()
                                          .SelectMany(x => x.GetCustomAttributes<IndexAttribute>())
                                          .ToList();

        foreach (var attribute in attributes) {
            var field = new Field(attribute.Name, attribute.Type, false, !attribute.Required, true);

            fields.Add(field);
        }
    }

    private static ITypesenseConverter GetElementConverter(Type type) {
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
            return TypesenseConverterRegistry.GetConverter(type.GetCollectionType());
        } else {
            return null;
        }
    }

    private static void ValidateDeclaredFieldType<TDocument>(PropertyInfo property,
                                                             FieldAttribute attribute,
                                                             ITypesenseConverter converter,
                                                             FieldType serializedType) {
        if (serializedType != attribute.Type) {
            throw new Exception($"Property {property.Name.Quote()} on type " +
                                $"{typeof(TDocument).GetFriendlyName()} declares Typesense field type " +
                                $"{attribute.Type} but {converter.GetType().GetFriendlyName()} serializes " +
                                $"it as {serializedType}");
        }
    }

    private static void ValidateDeclaredFieldTypes<TDocument>() {
        foreach (var property in typeof(TDocument).GetProperties()) {
            var attribute = property.GetCustomAttribute<FieldAttribute>();

            if (attribute == null || NestedFieldExpander.ShouldExpand(attribute)) {
                continue;
            }

            var converter = TypesenseConverterRegistry.GetConverter(property.PropertyType);

            if (converter != null) {
                ValidateDeclaredFieldType<TDocument>(property, attribute, converter, converter.FieldType);
            } else if (GetElementConverter(property.PropertyType) is ITypesenseConverter elementConverter) {
                var arrayFieldType = NestedFieldExpander.ToArrayFieldType(attribute.Name, elementConverter.FieldType);

                ValidateDeclaredFieldType<TDocument>(property, attribute, elementConverter, arrayFieldType);
            }
        }
    }

    private static void ValidateNoDuplicates<TDocument>(IReadOnlyList<Field> fields) {
        var duplicate = fields.GroupBy(x => x.Name).FirstOrDefault(x => x.Count() > 1);

        if (duplicate != null) {
            throw new Exception($"Type {typeof(TDocument).GetFriendlyName()} declares more than one Typesense field " +
                                $"named {duplicate.Key.Quote()}");
        }
    }
}
