using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Search.Typesense.Queries;

public class ParameterizedTypesenseText : Value {
    private const string AndOperator = "&&";
    private const string NoDocumentId = "__none__";
    private const string OrOperator = "||";

    private readonly List<TypesenseParameters> _parameters;
    private readonly Dictionary<string, int> _mergeSuffixes;

    private ParameterizedTypesenseText(string text,
                                       IEnumerable<TypesenseParameters> parameters,
                                       IReadOnlyDictionary<string, int> mergeSuffixes) {
        Text = text;
        _parameters = parameters.OrEmpty().ToList();
        _mergeSuffixes = mergeSuffixes.OrEmpty().ToDictionary(x => x.Key, x => x.Value);
    }

    public bool IsEmpty => !Text.HasValue();

    public static ParameterizedTypesenseText Create(string text) {
        return new ParameterizedTypesenseText(text, null, null);
    }

    public static ParameterizedTypesenseText Create<T>(Expression<Func<T, object>> pathExpression, string text) {
        return Create<T, object>(pathExpression, text);
    }

    public static ParameterizedTypesenseText Create<T, TKey>(Expression<Func<T, TKey>> pathExpression, string text) {
        return Create(TypesenseField.Get(pathExpression), text);
    }

    public static ParameterizedTypesenseText Create<T, TKey1, TKey2>(Expression<Func<T, TKey1>> pathExpression1,
                                                                     Expression<Func<T, TKey2>> pathExpression2,
                                                                     string text) {
        return Create(TypesenseField.Get(pathExpression1),
                      TypesenseField.Get(pathExpression2),
                      text);
    }

    public static ParameterizedTypesenseText Create<T, TKey1, TKey2, TKey3>(Expression<Func<T, TKey1>> pathExpression1,
                                                                           Expression<Func<T, TKey2>> pathExpression2,
                                                                           Expression<Func<T, TKey3>> pathExpression3,
                                                                           string text) {
        return Create(TypesenseField.Get(pathExpression1),
                      TypesenseField.Get(pathExpression2),
                      TypesenseField.Get(pathExpression3),
                      text);
    }

    public static ParameterizedTypesenseText Create(string path, string text) {
        return Create([path], text);
    }

    public static ParameterizedTypesenseText Create(string path1, string path2, string text) {
        return Create([path1, path2], text);
    }

    public static ParameterizedTypesenseText Create(string path1, string path2, string path3, string text) {
        return Create([path1, path2, path3], text);
    }

    public static ParameterizedTypesenseText Empty() {
        return new ParameterizedTypesenseText(null, null, null);
    }

    public static ParameterizedTypesenseText MatchesNothing() {
        return new ParameterizedTypesenseText($"id:={NoDocumentId}", null, null);
    }

    private static ParameterizedTypesenseText Create(string[] paths, string text) {
        if (paths.IsSingle()) {
            text = text.Replace("þ", paths.Single());
        } else {
            var index = 1;
            foreach (var path in paths) {
                text = text.Replace($"þ{index}", path);

                index++;
            }
        }

        return Create(text);
    }

    public ParameterizedTypesenseText And(ParameterizedTypesenseText other) {
        return Merge(other, AndOperator);
    }

    public ParameterizedTypesenseText Or(ParameterizedTypesenseText other) {
        return Merge(other, OrOperator);
    }

    public static ParameterizedTypesenseText operator &(ParameterizedTypesenseText lhs,
                                                        ParameterizedTypesenseText rhs) {
        var lhsClone = lhs.Clone();

        return lhsClone.Merge(rhs, AndOperator);
    }

    public static ParameterizedTypesenseText operator |(ParameterizedTypesenseText lhs,
                                                        ParameterizedTypesenseText rhs) {
        var lhsClone = lhs.Clone();

        return lhsClone.Merge(rhs, OrOperator);
    }

    public ParameterizedTypesenseText WithParameter<T>(string name, params T[] value) {
        if (!name.StartsWith("@")) {
            name = "@" + name;
        }

        var convert = CachedConverter<T>.Value;

        if (convert == null) {
            throw new Exception($"Could not find a converter for parameter of type {typeof(T).FullName.Quote()}");
        }

        var convertedValueList = value.Select(x => convert(x)).ToList();

        if (convertedValueList.Any(x => x == null)) {
            throw new Exception($"A null value cannot be expressed as a Typesense filter literal for " +
                                $"parameter {name.Quote()}");
        }

        var convertedValues = convertedValueList.ToCsv();

        var parameter = new TypesenseParameters(name, convertedValues);

        _parameters.Add(parameter);

        return this;
    }

    public override string ToString() {
        if (_parameters.None()) {
            return Text;
        } else {
            var valuesByName = _parameters.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().Value);
            var pattern = string.Join("|", valuesByName.Keys.OrderByDescending(x => x.Length).Select(Regex.Escape));

            return Regex.Replace(Text, pattern, m => valuesByName[m.Value]);
        }
    }

    private ParameterizedTypesenseText Merge(ParameterizedTypesenseText other, string op) {
        var clone = other.Clone();

        if (clone.IsEmpty) {
            return this;
        }

        if (IsEmpty) {
            Text = clone.Text;
            _parameters.Clear();
            _parameters.AddRange(clone.Parameters);
        } else {
            foreach (var parameter in _parameters) {
                if (clone.Parameters.Any(x => x.Name == parameter.Name)) {
                    var mergeSuffix = _mergeSuffixes.GetOrAddOrUpdate(parameter.Name,
                                                                      () => GetSuffix(clone, parameter.Name),
                                                                      suffix => suffix + 1);

                    clone.RenameParameter(parameter.Name, mergeSuffix);
                }
            }

            _parameters.AddRange(clone.Parameters);

            Text = $"({Text}) {op} ({clone.Text})";
        }

        return this;
    }

    private ParameterizedTypesenseText Clone() {
        var newParameters = _parameters.Select(x => new TypesenseParameters(x.Name, x.Value));

        return new ParameterizedTypesenseText(Text, newParameters, _mergeSuffixes);
    }

    private void RenameParameter(string name, int suffix) {
        var newName = $"{name}_{suffix}";
        var parameter = Parameters.Single(x => x.Name == name);

        parameter.Name = newName;
        Text = Regex.Replace(Text,
                             $"{name}([^a-z0-9_]|$)",
                             m => newName + m.Groups[1].Value,
                             RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private int GetSuffix(ParameterizedTypesenseText clause, string name) {
        for (var suffix = 1; true; suffix++) {
            var newName = $"{name}_{suffix}";

            if (!Parameters.Any(x => x.Name.EqualsInvariant(newName)) &&
                !clause.Parameters.Any(x => x.Name.EqualsInvariant(newName))) {
                return suffix;
            }
        }
    }

    public string Text { get; private set; }

    public IEnumerable<TypesenseParameters> Parameters => _parameters;

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Text;

        foreach (var parameter in _parameters.OrEmpty()) {
            yield return parameter;
        }

        foreach (var mergeSuffix in _mergeSuffixes.OrEmpty()) {
            yield return mergeSuffix;
        }
    }

    private static class CachedConverter<T> {
        public static readonly Func<object, string> Value = TypesenseParameterConverter.GetConverter(typeof(T));
    }
}
