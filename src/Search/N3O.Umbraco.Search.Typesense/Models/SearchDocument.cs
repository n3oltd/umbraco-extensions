using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Attributes;
using System;
using Typesense;

namespace N3O.Umbraco.Search.Typesense.Models;

public abstract class SearchDocument : Value {
    [Field("id", FieldType.String, true, true)]
    public string Id { get; set; }

    [Field("content_key", FieldType.String, true, true)]
    public Guid ContentKey { get; set; }

    [Field("culture", FieldType.String, false, true, facet: true)]
    public string Culture { get; set; }

    public static string GetId(Guid contentKey, string culture) {
        return culture.HasValue() ? $"{contentKey}_{culture}" : contentKey.ToString();
    }
}
