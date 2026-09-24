using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using System.Collections.Generic;
using System.Globalization;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimFilterParser {
    private const int MaxDepth = 20;
    private const int MaxLength = 2048;

    private readonly IReadOnlyList<ScimToken> _tokens;
    private int _depth;
    private int _index;

    private ScimFilterParser(IReadOnlyList<ScimToken> tokens) {
        _tokens = tokens;
    }

    public static ScimExpression Parse(string filter) {
        if (!filter.HasValue()) {
            return null;
        }

        // A stack overflow cannot be caught, so nesting is refused rather than allowed to reach the stack
        if (filter.Length > MaxLength) {
            throw ScimException.InvalidFilter($"A filter may not exceed {MaxLength} characters");
        }

        return Parse(ScimLexer.Tokenise(filter));
    }

    public static ScimExpression Parse(IReadOnlyList<ScimToken> tokens) {
        var parser = new ScimFilterParser(tokens);
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

            var inner = Nested();

            Expect(ScimTokenType.CloseParen);

            return new NotScimExpression(inner);
        }

        if (Current.Type == ScimTokenType.OpenParen) {
            _index++;

            var inner = Nested();

            Expect(ScimTokenType.CloseParen);

            return inner;
        }

        return ParseComparison();
    }

    private ScimExpression Nested() {
        if (++_depth > MaxDepth) {
            throw ScimException.InvalidFilter($"A filter may not nest more than {MaxDepth} deep");
        }

        try {
            return ParseOr();
        } finally {
            _depth--;
        }
    }

    private ScimExpression ParseComparison() {
        if (Current.Type != ScimTokenType.Identifier) {
            throw ScimException.InvalidFilter($"Expected an attribute but found {Describe(Current)}");
        }

        var path = ScimAttributePath.Parse(Current.Text);

        _index++;

        // A value path is meaningful when patching and not when querying, and answering one wrongly is
        // worse than refusing it
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

            case ScimTokenType.Number when decimal.TryParse(token.Text,
                                                           NumberStyles.Number,
                                                           CultureInfo.InvariantCulture,
                                                           out var number):
                return number;

            case ScimTokenType.Number:
                throw ScimException.InvalidFilter($"{token.Text.Quote()} is not a number");

            case ScimTokenType.Identifier when token.Text.Is("true"):
                return true;

            case ScimTokenType.Identifier when token.Text.Is("false"):
                return false;

            case ScimTokenType.Identifier when token.Text.Is("null"):
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
        return Current.Type == ScimTokenType.Keyword && Current.Text.Is(keyword);
    }

    private static string Describe(ScimToken token) {
        return token.Type == ScimTokenType.End ? "the end of the filter" : token.Text.Quote();
    }
}
