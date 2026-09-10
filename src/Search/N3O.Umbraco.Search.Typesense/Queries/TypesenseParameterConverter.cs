using N3O.Umbraco.Lookups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Reflection;

namespace N3O.Umbraco.Search.Typesense.Queries;

public static class TypesenseParameterConverter {
    public static Func<object, string> GetConverter(Type parameterType) {
        var documentConverter = TypesenseConverterRegistry.GetConverter(parameterType);

        if (documentConverter != null) {
            var convert = GetScalarConverter(documentConverter.UnderlyingTypesenseType);

            return convert == null ? null : v => convert(documentConverter.ToTypesenseValue(v));
        } else if (typeof(ILookup).IsAssignableFrom(parameterType)) {
            return v => Backtick(((ILookup) v)?.Id);
        } else {
            return GetScalarConverter(parameterType);
        }
    }

    private static string Backtick(string value) {
        return value == null ? null : $"`{value.Replace("`", "\\`")}`";
    }

    // Only the shapes Typesense accepts as a filter_by literal
    private static Func<object, string> GetScalarConverter(Type type) {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string)) {
            return v => Backtick((string) v);
        } else if (underlyingType == typeof(bool)) {
            return v => v == null ? null : (bool) v ? "true" : "false";
        } else if (underlyingType == typeof(Guid) ||
                   underlyingType == typeof(TimeSpan) ||
                   underlyingType == typeof(char)) {
            return v => v == null ? null : Backtick(Convert.ToString(v, CultureInfo.InvariantCulture));
        } else if (underlyingType.IsEnum) {
            var jsonConverterAttribute = underlyingType.GetCustomAttribute<JsonConverterAttribute>();

            if (jsonConverterAttribute != null) {
                var converterParameters = jsonConverterAttribute.ConverterParameters ?? [];
                var jsonConverter = (JsonConverter) Activator.CreateInstance(jsonConverterAttribute.ConverterType,
                                                                             converterParameters);

                return v => v == null ? null : ToLiteral(JsonConvert.SerializeObject(v, jsonConverter));
            } else {
                var integralType = Enum.GetUnderlyingType(underlyingType);

                return v => v == null ?
                            null :
                            Convert.ToString(Convert.ChangeType(v, integralType), CultureInfo.InvariantCulture);
            }
        } else if (IsNumeric(underlyingType)) {
            return v => v == null ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        } else {
            return null;
        }
    }

    private static bool IsNumeric(Type type) {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(decimal) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(ushort) ||
               type == typeof(uint) ||
               type == typeof(ulong);
    }

    private static string ToLiteral(string json) {
        var token = JToken.Parse(json);

        if (token.Type == JTokenType.String) {
            return Backtick(token.Value<string>());
        } else if (token.Type == JTokenType.Null) {
            return null;
        } else {
            return json;
        }
    }
}
