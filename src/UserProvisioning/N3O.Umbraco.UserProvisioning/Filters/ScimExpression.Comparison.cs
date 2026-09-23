using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Scim;
using System;
using System.Globalization;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ComparisonScimExpression : ScimExpression {
    public ComparisonScimExpression(ScimAttributePath path, string op, object value) {
        Operator = op.ToLowerInvariant();
        Path = path;
        Value = value;
    }

    public string Operator { get; }
    public ScimAttributePath Path { get; }
    public object Value { get; }

    public override bool Matches(ScimAttributes attributes) {
        return attributes.Read(Path).Any(Compare);
    }

    public override string ToString() {
        return $"{Path} {Operator} {Value}";
    }

    private bool Compare(object held) {
        if (held is bool heldFlag) {
            return Operator switch {
                "eq" => heldFlag == AsBool(Value),
                "ne" => heldFlag != AsBool(Value),
                _ => throw ScimException.InvalidFilter($"{Operator.Quote()} cannot be applied to {Path}")
            };
        }

        var left = AsString(held);
        var right = AsString(Value);

        return Operator switch {
            "co" => left.IndexOf(right, ScimText.Comparison) >= 0,
            "eq" => left.Is(right),
            "ew" => left.EndsWith(right, ScimText.Comparison),
            "ge" => Rank(left, right) >= 0,
            "gt" => Rank(left, right) > 0,
            "le" => Rank(left, right) <= 0,
            "lt" => Rank(left, right) < 0,
            "ne" => !left.Is(right),
            "sw" => left.StartsWith(right, ScimText.Comparison),
            _ => throw ScimException.InvalidFilter($"{Operator.Quote()} is not a comparison this endpoint applies")
        };
    }

    private static bool AsBool(object value) {
        return value is bool flag ? flag : bool.TryParse(AsString(value), out var parsed) && parsed;
    }

    private static string AsString(object value) {
        return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static int Rank(string left, string right) {
        return string.Compare(left, right, ScimText.Comparison);
    }
}
