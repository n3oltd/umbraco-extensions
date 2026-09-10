using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Search.Typesense.Attributes;
using N3O.Umbraco.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public static class NestedFieldExpander {
    private static readonly TypesenseJsonContractResolver ContractResolver = new();
    private static readonly Lazy<IReadOnlyList<JsonConverter>> JsonConverters = new(LoadJsonConverters);

    public static bool ShouldExpand(FieldAttribute attribute) {
        return attribute.Index && (attribute.Type == FieldType.Object || attribute.Type == FieldType.ObjectArray);
    }

    public static IReadOnlyList<Field> Expand(Type documentType, PropertyInfo property, FieldAttribute attribute) {
        ValidateOptions(documentType, attribute);

        var rootProperty = GetRootProperty(documentType, property, attribute);
        var objectType = GetObjectType(documentType, property, attribute);
        var isArray = attribute.Type == FieldType.ObjectArray;
        var fields = new List<Field>();
        var ancestry = new List<Type>();

        if (HasMemberConverter(rootProperty)) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared as " +
                                $"{attribute.Type} but {property.Name} has its own JsonConverter, so the shape it " +
                                "writes cannot be expanded into nested fields");
        } else if (!IsExpandableType(objectType)) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared as " +
                                $"{attribute.Type} but {objectType.GetFriendlyName()} cannot be expanded into " +
                                "nested fields");
        }

        // Typesense rejects a document that lacks a non-optional field
        var isRequired = attribute.Required && !isArray && IsAlwaysWritten(rootProperty);

        AddFieldsForType(objectType, attribute.Name, isArray, isRequired, ancestry, fields);

        ValidateFields(documentType, attribute.Name, fields);

        return fields;
    }

    public static FieldType ToArrayFieldType(string path, FieldType fieldType) {
        if (fieldType == FieldType.Bool) {
            return FieldType.BoolArray;
        } else if (fieldType == FieldType.Float) {
            return FieldType.FloatArray;
        } else if (fieldType == FieldType.Int32) {
            return FieldType.Int32Array;
        } else if (fieldType == FieldType.Int64) {
            return FieldType.Int64Array;
        } else if (fieldType == FieldType.String) {
            return FieldType.StringArray;
        } else {
            throw new Exception($"Cannot index {path.Quote()} because field type {fieldType} has no array form");
        }
    }

    private static void ValidateOptions(Type documentType, FieldAttribute attribute) {
        if (attribute.Facet ||
            attribute.Sort ||
            attribute.Infix ||
            attribute.Locale.HasValue() ||
            attribute.NumberOfDimensions != 0) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is expanded " +
                                "into its nested fields, so facet, sort, infix, locale and number of dimensions " +
                                "cannot be set on it");
        }
    }

    private static JsonProperty GetRootProperty(Type documentType, PropertyInfo property, FieldAttribute attribute) {
        var rootProperty = GetSerializedProperties(documentType).FirstOrDefault(x => x.UnderlyingName == property.Name);

        if (rootProperty == null) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared on " +
                                $"{property.Name}, which the serializer does not write");
        }

        return rootProperty;
    }

    private static Type GetObjectType(Type documentType, PropertyInfo property, FieldAttribute attribute) {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (IsDictionaryType(propertyType)) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared as " +
                                $"{attribute.Type} but {property.Name} is a dictionary, whose keys are data and so " +
                                "cannot be declared as fields");
        }

        var arrayContract = GetArrayContract(propertyType);

        if (attribute.Type == FieldType.ObjectArray) {
            if (arrayContract == null) {
                throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared " +
                                    $"as {attribute.Type} but {property.Name} is not a collection");
            } else if (arrayContract.CollectionItemType == null) {
                throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared " +
                                    $"as {attribute.Type} but the element type of {property.Name} cannot be " +
                                    "determined");
            }

            return arrayContract.CollectionItemType;
        } else if (arrayContract != null) {
            throw new Exception($"Field {attribute.Name.Quote()} on {documentType.GetFriendlyName()} is declared as " +
                                $"{attribute.Type} but {property.Name} is a collection");
        } else {
            return propertyType;
        }
    }

    private static void ValidateFields(Type documentType, string name, IReadOnlyList<Field> fields) {
        if (fields.None()) {
            throw new Exception($"Field {name.Quote()} on {documentType.GetFriendlyName()} expanded to no nested " +
                                "fields because none of its properties have a Typesense field type");
        }
    }

    private static void AddFieldsForType(Type type,
                                         string path,
                                         bool isArray,
                                         bool isRequired,
                                         List<Type> ancestry,
                                         List<Field> fields) {
        if (ancestry.Contains(type)) {
            return;
        } else if (ancestry.Count == MaxDepth) {
            throw new Exception($"Field {path.Quote()} is nested more than {MaxDepth} levels deep, which means its " +
                                "type expands without terminating");
        }

        ancestry.Add(type);

        foreach (var property in GetSerializedProperties(type)) {
            var propertyPath = $"{path}{TypesenseField.PathSeparator}{property.PropertyName}";

            AddFieldsForProperty(property, propertyPath, isArray, isRequired, ancestry, fields);
        }

        ancestry.RemoveAt(ancestry.Count - 1);
    }

    private static void AddFieldsForProperty(JsonProperty property,
                                             string path,
                                             bool isArray,
                                             bool isRequired,
                                             List<Type> ancestry,
                                             List<Field> fields) {
        var propertyType = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isRequiredHere = isRequired && IsAlwaysWritten(property);

        if (HasMemberConverter(property)) {
            return;
        } else if (GetFieldType(propertyType) is FieldType fieldType) {
            fields.Add(CreateField(path, fieldType, isArray, isRequiredHere));
        } else if (typeof(JToken).IsAssignableFrom(underlyingType) || IsDictionaryType(underlyingType)) {
            return;
        } else if (GetArrayContract(underlyingType) is JsonArrayContract arrayContract) {
            AddFieldsForCollection(arrayContract, path, ancestry, fields);
        } else if (IsExpandableType(underlyingType)) {
            AddFieldsForType(underlyingType, path, isArray, isRequiredHere, ancestry, fields);
        }
    }

    private static void AddFieldsForCollection(JsonArrayContract contract,
                                               string path,
                                               List<Type> ancestry,
                                               List<Field> fields) {
        var elementType = contract.CollectionItemType;

        if (elementType == null) {
            return;
        }

        var underlyingElementType = Nullable.GetUnderlyingType(elementType) ?? elementType;

        if (underlyingElementType == typeof(byte)) {
            return;
        } else if (GetFieldType(elementType) is FieldType elementFieldType) {
            fields.Add(CreateField(path, elementFieldType, true, false));
        } else if (IsExpandableType(underlyingElementType)) {
            AddFieldsForType(underlyingElementType, path, true, false, ancestry, fields);
        }
    }

    private static IReadOnlyList<JsonProperty> GetSerializedProperties(Type type) {
        return GetContract(type).Properties
                                .Where(x => !x.Ignored && x.Readable)
                                .ToList();
    }

    private static JsonObjectContract GetContract(Type type) {
        return ContractResolver.ResolveContract(type) as JsonObjectContract;
    }

    private static JsonArrayContract GetArrayContract(Type type) {
        return ContractResolver.ResolveContract(type) as JsonArrayContract;
    }

    private static FieldType? GetFieldType(Type type) {
        var converter = TypesenseConverterRegistry.GetConverter(type);

        if (converter != null) {
            return converter.FieldType;
        } else if (IsIdLookup(Nullable.GetUnderlyingType(type) ?? type)) {
            return FieldType.String;
        } else {
            return GetScalarFieldType(type);
        }
    }

    private static bool IsIdLookup(Type type) {
        return !type.IsInterface && type.IsLookup() && !IsBaseLookup(type);
    }

    private static bool IsBaseLookup(Type type) {
        return type.IsAnyOf(typeof(Lookup), typeof(NamedLookup));
    }

    private static bool IsKnownAtCompileTime(Type type) {
        return !type.IsInterface && !type.IsAbstract;
    }

    private static FieldType? GetScalarFieldType(Type type) {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var typeCode = Type.GetTypeCode(underlyingType);

        if (underlyingType.IsEnum && HasJsonConverterFor(underlyingType)) {
            return null;
        } else if (underlyingType == typeof(Guid) || underlyingType == typeof(TimeSpan)) {
            return FieldType.String;
        } else if (typeCode == TypeCode.Boolean) {
            return FieldType.Bool;
        } else if (typeCode.IsAnyOf(TypeCode.Byte, TypeCode.Int16, TypeCode.Int32, TypeCode.SByte, TypeCode.UInt16)) {
            return FieldType.Int32;
        } else if (typeCode.IsAnyOf(TypeCode.Int64, TypeCode.UInt32, TypeCode.UInt64)) {
            return FieldType.Int64;
        } else if (typeCode.IsAnyOf(TypeCode.Decimal, TypeCode.Double, TypeCode.Single)) {
            return FieldType.Float;
        } else if (typeCode.IsAnyOf(TypeCode.Char, TypeCode.String)) {
            return FieldType.String;
        } else {
            return null;
        }
    }

    private static bool IsExpandableType(Type type) {
        return !type.IsEnum &&
               IsKnownAtCompileTime(type) &&
               !IsBaseLookup(type) &&
               OurAssemblies.IsOurAssembly(type.Assembly) &&
               TypesenseConverterRegistry.GetConverter(type) == null &&
               !HasJsonConverterFor(type) &&
               GetContract(type) != null;
    }

    private static bool HasJsonConverterFor(Type type) {
        return type.HasAttribute<JsonConverterAttribute>() ||
               JsonConverters.Value.Any(x => x.CanWrite && x.CanConvert(type));
    }

    private static bool HasMemberConverter(JsonProperty property) {
        return (property.ItemConverter != null && !(property.ItemConverter is TypesenseDispatchJsonConverter)) ||
               (property.Converter != null && !(property.Converter is TypesenseDispatchJsonConverter));
    }

    private static bool IsDictionaryType(Type type) {
        return ContractResolver.ResolveContract(type) is JsonDictionaryContract;
    }

    // Runs before the container exists
    private static IReadOnlyList<JsonConverter> LoadJsonConverters() {
        return OurAssemblies.GetTypes(x => x.IsConcreteClass() &&
                                           !x.IsGenericTypeDefinition &&
                                           x.IsSubclassOfType(typeof(JsonConverter)) &&
                                           x.HasParameterlessConstructor())
                            .Select(x => (JsonConverter) Activator.CreateInstance(x))
                            .ToList();
    }

    private static bool IsAlwaysWritten(JsonProperty property) {
        return property.ShouldSerialize == null &&
               property.GetIsSpecified == null &&
               !property.DefaultValueHandling.GetValueOrDefault().HasFlag(DefaultValueHandling.Ignore) &&
               IsAlwaysWritten(property.PropertyType);
    }

    private static bool IsAlwaysWritten(Type type) {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
    }

    private static Field CreateField(string path, FieldType fieldType, bool isArray, bool isRequired) {
        var type = isArray ? ToArrayFieldType(path, fieldType) : fieldType;

        return new Field(path, type, false, !isRequired, true);
    }

    private const int MaxDepth = 16;
}
