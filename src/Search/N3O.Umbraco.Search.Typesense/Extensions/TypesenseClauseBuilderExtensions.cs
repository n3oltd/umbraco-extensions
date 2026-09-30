using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense.Extensions;

public static class TypesenseClauseBuilderExtensions {
    public static ITypesenseClauseBuilder<T> Eq<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         TKey value)
        where T : SearchDocument {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> NotEq<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                            Expression<Func<T, TKey>> pathExpression,
                                                            TKey value)
        where T : SearchDocument {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:!=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> In<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         IEnumerable<TKey> values)
        where T : SearchDocument {
        var array = values.OrEmpty().ToArray();

        if (array.Length == 0) {
            return c;
        }

        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:=[@v]")
                                               .WithParameter("v", array);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> NotIn<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                            Expression<Func<T, TKey>> pathExpression,
                                                            IEnumerable<TKey> values)
        where T : SearchDocument {
        var array = values.OrEmpty().ToArray();

        if (array.Length == 0) {
            return c;
        }

        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:!=[@v]")
                                               .WithParameter("v", array);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Gt<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey?>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:>@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Gte<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                          Expression<Func<T, TKey?>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:>=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Lt<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey?>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:<@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Lte<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                          Expression<Func<T, TKey?>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:<=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Between<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                              Expression<Func<T, TKey?>> pathExpression,
                                                              TKey min,
                                                              TKey max)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:[@min..@max]")
                                               .WithParameter("min", min)
                                               .WithParameter("max", max);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Between<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                              Expression<Func<T, TKey?>> pathExpression,
                                                              Range<TKey?> range)
        where T : SearchDocument
        where TKey : struct {
        if (range == null || (!range.From.HasValue && !range.To.HasValue)) {
            return c;
        }

        if (range.From.HasValue && !range.To.HasValue) {
            return c.Gte(pathExpression, range.From.GetValueOrThrow());
        }

        if (!range.From.HasValue && range.To.HasValue) {
            return c.Lte(pathExpression, range.To.GetValueOrThrow());
        }

        return c.Between(pathExpression, range.From.GetValueOrThrow(), range.To.GetValueOrThrow());
    }

    public static ITypesenseClauseBuilder<T> StartsWith<T>(this ITypesenseClauseBuilder<T> c,
                                                           Expression<Func<T, string>> pathExpression,
                                                           string prefix)
        where T : SearchDocument {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:=@v*")
                                               .WithParameter("v", prefix);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Partial<T>(this ITypesenseClauseBuilder<T> c,
                                                        Expression<Func<T, string>> pathExpression,
                                                        string word)
        where T : SearchDocument {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:@v")
                                               .WithParameter("v", word);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Contains<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                               Expression<Func<T, IEnumerable<TKey>>> pathExpression,
                                                               TKey value)
        where T : SearchDocument {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> ContainsAny<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                                  Expression<Func<T, IEnumerable<TKey>>> pathExpression,
                                                                  IEnumerable<TKey> values)
        where T : SearchDocument {
        var array = values.OrEmpty().ToArray();

        if (array.Length == 0) {
            return c;
        }

        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:=[@v]")
                                               .WithParameter("v", array);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> ContainsAll<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                                  Expression<Func<T, IEnumerable<TKey>>> pathExpression,
                                                                  IEnumerable<TKey> values)
        where T : SearchDocument {
        foreach (var value in values.OrEmpty()) {
            c.Contains(pathExpression, value);
        }

        return c;
    }

    public static ITypesenseClauseBuilder<T> Gt<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:>@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Gte<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                          Expression<Func<T, TKey>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:>=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Lt<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:<@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Lte<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                          Expression<Func<T, TKey>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:<=@v")
                                               .WithParameter("v", value);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Between<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                              Expression<Func<T, TKey>> pathExpression,
                                                              TKey min,
                                                              TKey max)
        where T : SearchDocument
        where TKey : struct {
        var clause = ParameterizedTypesenseText.Create(pathExpression, "þ:[@min..@max]")
                                               .WithParameter("min", min)
                                               .WithParameter("max", max);

        return c.And(clause);
    }

    public static ITypesenseClauseBuilder<T> Between<T, TKey>(this ITypesenseClauseBuilder<T> c,
                                                              Expression<Func<T, TKey>> pathExpression,
                                                              Range<TKey?> range)
        where T : SearchDocument
        where TKey : struct {
        if (range == null || (!range.From.HasValue && !range.To.HasValue)) {
            return c;
        }

        if (range.From.HasValue && !range.To.HasValue) {
            return c.Gte(pathExpression, range.From.GetValueOrThrow());
        }

        if (!range.From.HasValue && range.To.HasValue) {
            return c.Lte(pathExpression, range.To.GetValueOrThrow());
        }

        return c.Between(pathExpression, range.From.GetValueOrThrow(), range.To.GetValueOrThrow());
    }
}
