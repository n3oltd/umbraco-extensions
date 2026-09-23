using N3O.Umbraco.UserProvisioning.Scim;
using System.Collections.Generic;
using System.Text;

namespace N3O.Umbraco.UserProvisioning.Filters;

public static class ScimLexer {
    public static IReadOnlyList<ScimToken> Tokenise(string text) {
        var tokens = new List<ScimToken>();
        var index = 0;

        while (index < text.Length) {
            var c = text[index];

            if (char.IsWhiteSpace(c)) {
                index++;
            } else if (c == '(') {
                tokens.Add(new ScimToken(ScimTokenType.OpenParen, "("));
                index++;
            } else if (c == ')') {
                tokens.Add(new ScimToken(ScimTokenType.CloseParen, ")"));
                index++;
            } else if (c == '[') {
                tokens.Add(new ScimToken(ScimTokenType.OpenBracket, "["));
                index++;
            } else if (c == ']') {
                tokens.Add(new ScimToken(ScimTokenType.CloseBracket, "]"));
                index++;
            } else if (c == '"') {
                tokens.Add(new ScimToken(ScimTokenType.String, ReadString(text, ref index)));
            } else if (char.IsDigit(c) || c == '-') {
                tokens.Add(new ScimToken(ScimTokenType.Number, ReadWhile(text, ref index, x => char.IsDigit(x) ||
                                                                                               x == '.' ||
                                                                                               x == '-')));
            } else {
                var word = ReadWhile(text, ref index, x => char.IsLetterOrDigit(x) ||
                                                           x == '_' ||
                                                           x == '-' ||
                                                           x == '.' ||
                                                           x == ':' ||
                                                           x == '$');

                if (word.Length == 0) {
                    throw ScimException.InvalidFilter($"Unexpected character '{c}' at position {index}");
                }

                tokens.Add(new ScimToken(IsKeyword(word) ? ScimTokenType.Keyword : ScimTokenType.Identifier, word));
            }
        }

        tokens.Add(new ScimToken(ScimTokenType.End, null));

        return tokens;
    }

    private static bool IsKeyword(string word) {
        switch (word.ToLowerInvariant()) {
            case "and":
            case "co":
            case "eq":
            case "ew":
            case "ge":
            case "gt":
            case "le":
            case "lt":
            case "ne":
            case "not":
            case "or":
            case "pr":
            case "sw":
                return true;

            default:
                return false;
        }
    }

    private static string ReadString(string text, ref int index) {
        var builder = new StringBuilder();

        index++;

        while (index < text.Length && text[index] != '"') {
            if (text[index] == '\\' && index + 1 < text.Length) {
                index++;

                builder.Append(text[index] switch {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => text[index]
                });
            } else {
                builder.Append(text[index]);
            }

            index++;
        }

        if (index >= text.Length) {
            throw ScimException.InvalidFilter("Unterminated string");
        }

        index++;

        return builder.ToString();
    }

    private static string ReadWhile(string text, ref int index, System.Func<char, bool> predicate) {
        var start = index;

        while (index < text.Length && predicate(text[index])) {
            index++;
        }

        return text.Substring(start, index - start);
    }
}
