using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.ContentTypes;

public class ContentTypeKindMismatchException : Exception {
    public ContentTypeKindMismatchException(string alias, bool isElement)
        : base($"Content type {alias.Quote()} is held by the site as " +
               $"{(isElement ? "an element" : "a document")} type, so it cannot be converged as " +
               $"{(isElement ? "a document" : "an element")} type") {
        Alias = alias;
        IsElement = isElement;
    }

    public string Alias { get; }
    public bool IsElement { get; }
}
