using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.ContentTypes;

public class ContentTypeKeyMismatchException : Exception {
    public ContentTypeKeyMismatchException(string alias, Guid expectedKey, Guid actualKey)
        : base($"Content type {alias.Quote()} is held by the site under key {actualKey}, not the key {expectedKey} " +
               "this designer owns, so it cannot be converged") {
        Alias = alias;
        ExpectedKey = expectedKey;
        ActualKey = actualKey;
    }

    public string Alias { get; }
    public Guid ExpectedKey { get; }
    public Guid ActualKey { get; }
}
