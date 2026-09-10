using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace N3O.Umbraco.Search.Typesense;

public static class TypesenseField {
    private static readonly NamingStrategy CamelCase = new CamelCaseNamingStrategy();

    public static string Get<T, TField>(Expression<Func<T, TField>> pathExpression) {
        var pathComponents = new List<string>();
        var memberExpression = GetMemberExpression(pathExpression.Body);

        if (memberExpression == null) {
            throw new Exception($"The path {pathExpression.Body.ToString().Quote()} is not a property path");
        }

        while (memberExpression != null) {
            var propertyName = ToSearchFieldName(memberExpression.Member);

            pathComponents.Insert(0, propertyName);

            memberExpression = memberExpression.Expression as MemberExpression;
        }

        return string.Join(PathSeparator, pathComponents);
    }

    public static string ToSearchFieldName(MemberInfo memberInfo) {
        var propertyInfo = memberInfo as PropertyInfo;

        var fieldAttribute = propertyInfo?.GetCustomAttribute<FieldAttribute>();
        if (fieldAttribute != null) {
            return fieldAttribute.Name;
        }

        var jsonProperty = propertyInfo?.GetCustomAttribute<JsonPropertyAttribute>();
        if (jsonProperty?.PropertyName.HasValue() == true) {
            return jsonProperty.PropertyName;
        }

        return CamelCase.GetPropertyName(memberInfo.Name, false);
    }

    private static MemberExpression GetMemberExpression(Expression body) {
        if (body.NodeType.IsAnyOf(ExpressionType.Convert, ExpressionType.ConvertChecked)) {
            return (body as UnaryExpression)?.Operand as MemberExpression;
        } else {
            return body as MemberExpression;
        }
    }

    public const char PathSeparator = '.';
}
