using N3O.Umbraco.UserProvisioning.Scim;
using System;
using System.Collections.Generic;
using System.Text;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimLexer {
    private readonly string _text;
    private int _index;

    private ScimLexer(string text) {
        _text = text;
    }

    public static IReadOnlyList<ScimToken> Tokenise(string text) {
        return new ScimLexer(text).Run();
    }

    private IReadOnlyList<ScimToken> Run() {
        var tokens = new List<ScimToken>();

        while (_index < _text.Length) {
            var c = _text[_index];

            if (char.IsWhiteSpace(c)) {
                _index++;
            } else if (c == '(') {
                tokens.Add(Single(ScimTokenType.OpenParen, "("));
            } else if (c == ')') {
                tokens.Add(Single(ScimTokenType.CloseParen, ")"));
            } else if (c == '[') {
                tokens.Add(Single(ScimTokenType.OpenBracket, "["));
            } else if (c == ']') {
                tokens.Add(Single(ScimTokenType.CloseBracket, "]"));
            } else if (c == '"') {
                tokens.Add(new ScimToken(ScimTokenType.String, ReadString()));
            } else if (char.IsDigit(c) || c == '-') {
                tokens.Add(new ScimToken(ScimTokenType.Number,
                                         ReadWhile(x => char.IsDigit(x) || x == '.' || x == '-')));
            } else {
                var word = ReadWhile(x => char.IsLetterOrDigit(x) ||
                                          x == '_' ||
                                          x == '-' ||
                                          x == '.' ||
                                          x == ':' ||
                                          x == '$');

                if (word.Length == 0) {
                    throw ScimException.InvalidFilter($"Unexpected character '{c}' at position {_index}");
                }

                tokens.Add(new ScimToken(IsKeyword(word) ? ScimTokenType.Keyword : ScimTokenType.Identifier, word));
            }
        }

        tokens.Add(new ScimToken(ScimTokenType.End, null));

        return tokens;
    }

    private string ReadString() {
        var builder = new StringBuilder();

        _index++;

        while (_index < _text.Length && _text[_index] != '"') {
            if (_text[_index] == '\\' && _index + 1 < _text.Length) {
                _index++;

                builder.Append(_text[_index] switch {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => _text[_index]
                });
            } else {
                builder.Append(_text[_index]);
            }

            _index++;
        }

        if (_index >= _text.Length) {
            throw ScimException.InvalidFilter("Unterminated string");
        }

        _index++;

        return builder.ToString();
    }

    private string ReadWhile(Func<char, bool> predicate) {
        var start = _index;

        while (_index < _text.Length && predicate(_text[_index])) {
            _index++;
        }

        return _text.Substring(start, _index - start);
    }

    private ScimToken Single(ScimTokenType type, string text) {
        _index++;

        return new ScimToken(type, text);
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
}
