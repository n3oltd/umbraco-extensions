using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Scim;
using System.Collections.Generic;
using System.Globalization;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimFilterParser {
    private readonly IReadOnlyList<ScimToken> _tokens;
    private int _index;

    private ScimFilterParser(IReadOnlyList<ScimToken> tokens) {
        _tokens = tokens;
    }

    public static ScimExpression Parse(string filter) {
        if (!filter.HasValue()) {
            return null;
        }

        var parser = new ScimFilterParser(ScimLexer.Tokenise(filter));
        var expression = parser.ParseOr();

        parser.Expect(ScimTokenType.End);

        return expression;
    }

    private ScimToken Current => _tokens[_index];

    private ScimExpression ParseOr() {
        var left = ParseAnd();

        while (IsKeyword("or")) {
            _index++;

            left = new OrScimExpression(left, ParseAnd());
        }

        return left;
    }

    private ScimExpression ParseAnd() {
        var left = ParseUnary();

        while (IsKeyword("and")) {
            _index++;

            left = new AndScimExpression(left, ParseUnary());
        }

        return left;
    }

    private ScimExpression ParseUnary() {
        if (IsKeyword("not")) {
            _index++;

            Expect(ScimTokenType.OpenParen);

            var inner = ParseOr();

            Expect(ScimTokenType.CloseParen);

            return new NotScimExpression(inner);
        }

        if (Current.Type == ScimTokenType.OpenParen) {
            _index++;

            var inner = ParseOr();

            Expect(ScimTokenType.CloseParen);

            return inner;
        }

        return ParseComparison();
    }

    private ScimExpression ParseComparison() {
        if (Current.Type != ScimTokenType.Identifier) {
            throw ScimException.InvalidFilter($"Expected an attribute but found {Describe(Current)}");
        }

        var path = ScimAttributePath.Parse(Current.Text);

        _index++;

        // A value path filters inside a multi-valued attribute. It is meaningful when patching and not
        // when querying, and answering one wrongly is worse than refusing it
        if (Current.Type == ScimTokenType.OpenBracket) {
            throw ScimException.InvalidFilter($"A filter on the members of {path} is not applied by this endpoint");
        }

        if (IsKeyword("pr")) {
            _index++;

            return new PresentScimExpression(path);
        }

        if (Current.Type != ScimTokenType.Keyword) {
            throw ScimException.InvalidFilter($"Expected a comparison after {path} but found {Describe(Current)}");
        }

        var op = Current.Text;

        _index++;

        return new ComparisonScimExpression(path, op, ParseValue());
    }

    private object ParseValue() {
        var token = Current;

        _index++;

        switch (token.Type) {
            case ScimTokenType.String:
                return token.Text;

            case ScimTokenType.Number:
                return decimal.Parse(token.Text, CultureInfo.InvariantCulture);

            case ScimTokenType.Identifier when token.Text.EqualsInvariant("true"):
                return true;

            case ScimTokenType.Identifier when token.Text.EqualsInvariant("false"):
                return false;

            case ScimTokenType.Identifier when token.Text.EqualsInvariant("null"):
                return null;

            default:
                throw ScimException.InvalidFilter($"Expected a value but found {Describe(token)}");
        }
    }

    private void Expect(ScimTokenType type) {
        if (Current.Type != type) {
            throw ScimException.InvalidFilter($"Expected {type} but found {Describe(Current)}");
        }

        _index++;
    }

    private bool IsKeyword(string keyword) {
        return Current.Type == ScimTokenType.Keyword && Current.Text.EqualsInvariant(keyword);
    }

    private static string Describe(ScimToken token) {
        return token.Type == ScimTokenType.End ? "the end of the filter" : token.Text.Quote();
    }
}
