using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense.Extensions;

public static class TypesenseSearchBuilderExtensions {
    public static ITypesenseSearchBuilder<T> Eq<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         TKey value)
        where T : SearchDocument {
        return q.Filter(c => c.Eq(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> NotEq<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                            Expression<Func<T, TKey>> pathExpression,
                                                            TKey value)
        where T : SearchDocument {
        return q.Filter(c => c.NotEq(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> In<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                         Expression<Func<T, TKey>> pathExpression,
                                                         IEnumerable<TKey> values)
        where T : SearchDocument {
        return q.Filter(c => c.In(pathExpression, values));
    }

    public static ITypesenseSearchBuilder<T> NotIn<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                            Expression<Func<T, TKey>> pathExpression,
                                                            IEnumerable<TKey> values)
        where T : SearchDocument {
        return q.Filter(c => c.NotIn(pathExpression, values));
    }

    public static ITypesenseSearchBuilder<T> Gt<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                         Expression<Func<T, TKey?>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Gt(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> Gte<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                          Expression<Func<T, TKey?>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Gte(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> Lt<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                         Expression<Func<T, TKey?>> pathExpression,
                                                         TKey value)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Lt(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> Lte<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                          Expression<Func<T, TKey?>> pathExpression,
                                                          TKey value)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Lte(pathExpression, value));
    }

    public static ITypesenseSearchBuilder<T> Between<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                              Expression<Func<T, TKey?>> pathExpression,
                                                              TKey min,
                                                              TKey max)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Between(pathExpression, min, max));
    }

    public static ITypesenseSearchBuilder<T> Between<T, TKey>(this ITypesenseSearchBuilder<T> q,
                                                              Expression<Func<T, TKey?>> pathExpression,
                                                              Range<TKey?> range)
        where T : SearchDocument
        where TKey : struct {
        return q.Filter(c => c.Between(pathExpression, range));
    }

    public static ITypesenseSearchBuilder<T> StartsWith<T>(this ITypesenseSearchBuilder<T> q,
                                                           Expression<Func<T, string>> pathExpression,
                                                           string prefix)
        where T : SearchDocument {
        return q.Filter(c => c.StartsWith(pathExpression, prefix));
    }

    public static ITypesenseSearchBuilder<T> Partial<T>(this ITypesenseSearchBuilder<T> q,
                                                        Expression<Func<T, string>> pathExpression,
                                                        string word)
        where T : SearchDocument {
        return q.Filter(c => c.Partial(pathExpression, word));
    }
}
