using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense.Extensions;

public static class TypesenseSearchShapingExtensions {
    public static ITypesenseSearchBuilder<T> Page<T>(this ITypesenseSearchBuilder<T> q, int page, int perPage)
        where T : SearchDocument {
        return q.Custom(p => {
            p.Page = page;
            p.PerPage = perPage;
        });
    }

    public static ITypesenseSearchBuilder<T> Limit<T>(this ITypesenseSearchBuilder<T> q, int limit)
        where T : SearchDocument {
        return q.Custom(p => p.PerPage = limit);
    }

    public static ITypesenseSearchBuilder<T> Highlight<T, TField>(this ITypesenseSearchBuilder<T> q,
                                                                  Expression<Func<T, TField>> pathExpression)
        where T : SearchDocument {
        var field = TypesenseField.Get(pathExpression);

        return q.Custom(p => p.HighlightFields = AppendCsv(p.HighlightFields, field));
    }

    public static ITypesenseSearchBuilder<T> HighlightFull<T, TField>(this ITypesenseSearchBuilder<T> q,
                                                                      Expression<Func<T, TField>> pathExpression)
        where T : SearchDocument {
        var field = TypesenseField.Get(pathExpression);

        return q.Custom(p => p.HighlightFullFields = AppendCsv(p.HighlightFullFields, field));
    }

    public static ITypesenseSearchBuilder<T> HighlightTag<T>(this ITypesenseSearchBuilder<T> q,
                                                             string startTag,
                                                             string endTag)
        where T : SearchDocument {
        return q.Custom(p => {
            p.HighlightStartTag = startTag;
            p.HighlightEndTag = endTag;
        });
    }

    public static ITypesenseSearchBuilder<T> FacetBy<T, TField>(this ITypesenseSearchBuilder<T> q,
                                                                Expression<Func<T, TField>> pathExpression)
        where T : SearchDocument {
        var field = TypesenseField.Get(pathExpression);

        return q.Custom(p => p.FacetBy = AppendCsv(p.FacetBy, field));
    }

    public static ITypesenseSearchBuilder<T> MaxFacetValues<T>(this ITypesenseSearchBuilder<T> q, int max)
        where T : SearchDocument {
        return q.Custom(p => p.MaxFacetValues = max);
    }

    public static ITypesenseSearchBuilder<T> Include<T, TField>(this ITypesenseSearchBuilder<T> q,
                                                                Expression<Func<T, TField>> pathExpression)
        where T : SearchDocument {
        var field = TypesenseField.Get(pathExpression);

        return q.Custom(p => p.IncludeFields = AppendCsv(p.IncludeFields, field));
    }

    public static ITypesenseSearchBuilder<T> Exclude<T, TField>(this ITypesenseSearchBuilder<T> q,
                                                                Expression<Func<T, TField>> pathExpression)
        where T : SearchDocument {
        var field = TypesenseField.Get(pathExpression);

        return q.Custom(p => p.ExcludeFields = AppendCsv(p.ExcludeFields, field));
    }

    public static ITypesenseSearchBuilder<T> MaxTypos<T>(this ITypesenseSearchBuilder<T> q, int max)
        where T : SearchDocument {
        return q.Custom(p => p.NumberOfTypos = max.ToString(CultureInfo.InvariantCulture));
    }

    public static ITypesenseSearchBuilder<T> PrefixMatching<T>(this ITypesenseSearchBuilder<T> q, bool enabled = true)
        where T : SearchDocument {
        return q.Custom(p => p.Prefix = enabled);
    }

    private static string AppendCsv(string existing, string newValue) {
        return existing.HasValue() ? $"{existing},{newValue}" : newValue;
    }
}
