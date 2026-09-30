using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.ContentTypes;

public class ContentTypeNotFoundException : Exception {
    public ContentTypeNotFoundException(string alias) : base($"No content type found with alias {alias.Quote()}") {
        Alias = alias;
    }

    public string Alias { get; }
}
