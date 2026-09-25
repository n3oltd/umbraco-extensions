using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimPath {
    private const int MaxLength = 1024;

    private ScimPath(ScimAttributePath attribute, ScimExpression valueFilter, string subAttribute) {
        Attribute = attribute;
        SubAttribute = subAttribute;
        ValueFilter = valueFilter;
    }

    public ScimAttributePath Attribute { get; }
    public string SubAttribute { get; }
    public ScimExpression ValueFilter { get; }

    public static ScimPath Parse(string text) {
        if (!text.HasValue()) {
            return null;
        }

        if (text.Length > MaxLength) {
            throw ScimException.InvalidPath($"A path may not exceed {MaxLength} characters");
        }

        var tokens = ScimLexer.Tokenise(text);
        var index = 0;

        if (TypeAt(tokens, index) != ScimTokenType.Identifier) {
            throw ScimException.InvalidPath($"{text.Quote()} does not begin with an attribute");
        }

        var attribute = ScimAttributePath.Parse(tokens[index].Text);

        index++;

        ScimExpression valueFilter = null;

        if (TypeAt(tokens, index) == ScimTokenType.OpenBracket) {
            var depth = 0;
            var start = ++index;

            while (index < tokens.Count && !(tokens[index].Type == ScimTokenType.CloseBracket && depth == 0)) {
                if (tokens[index].Type == ScimTokenType.OpenBracket) {
                    depth++;
                } else if (tokens[index].Type == ScimTokenType.CloseBracket) {
                    depth--;
                }

                index++;
            }

            if (TypeAt(tokens, index) != ScimTokenType.CloseBracket) {
                throw ScimException.InvalidPath($"{text.Quote()} has no closing bracket");
            }

            valueFilter = ScimFilterParser.Parse(tokens.Skip(start).Take(index - start).ToList());

            index++;
        }

        string subAttribute = null;

        if (TypeAt(tokens, index) == ScimTokenType.Identifier) {
            subAttribute = tokens[index].Text.TrimStart('.');

            index++;
        }

        if (TypeAt(tokens, index) != ScimTokenType.End) {
            throw ScimException.InvalidPath($"{text.Quote()} carries more than one attribute path");
        }

        return new ScimPath(attribute, valueFilter, subAttribute);
    }

    public bool Is(string attribute) {
        return Attribute.Is(attribute);
    }

    public override string ToString() {
        var filter = ValueFilter == null ? "" : $"[{ValueFilter}]";
        var sub = SubAttribute.HasValue() ? $".{SubAttribute}" : "";

        return $"{Attribute}{filter}{sub}";
    }

    private static ScimTokenType TypeAt(IReadOnlyList<ScimToken> tokens, int index) {
        return index < tokens.Count ? tokens[index].Type : ScimTokenType.End;
    }
}
