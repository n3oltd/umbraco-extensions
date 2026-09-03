using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.ContentTypes;

public class DataTypeNotFoundException : Exception {
    public DataTypeNotFoundException(string nameOrKey, string contentTypeAlias, string propertyAlias)
        : base($"No data type found named {nameOrKey.Quote()} for property {propertyAlias.Quote()} on " +
               $"content type {contentTypeAlias.Quote()}") {
        NameOrKey = nameOrKey;
        ContentTypeAlias = contentTypeAlias;
        PropertyAlias = propertyAlias;
    }

    public string ContentTypeAlias { get; }
    public string NameOrKey { get; }
    public string PropertyAlias { get; }
}
