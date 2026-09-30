using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace N3O.Umbraco.Cloud.Extensions;

public static class EnumExtensions {
    public static T FromEnumString<T>(this string value) where T : struct, Enum {
        var field = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
                             .SingleOrDefault(x => x.GetCustomAttribute<EnumMemberAttribute>()?.Value == value);

        if (field == null) {
            throw new ArgumentException($"{value} is not a {typeof(T).Name} value", nameof(value));
        }

        return (T) field.GetValue(null);
    }

    public static string ToEnumString<T>(this T value) where T : struct, Enum {
        var enumType = typeof(T);
        var name = Enum.GetName(enumType, value);
        var enumMemberAttribute = enumType.GetField(name).GetCustomAttribute<EnumMemberAttribute>();

        return enumMemberAttribute?.Value;
    }
}
