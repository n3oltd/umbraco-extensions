using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.Cloud.Exceptions;

public class PublishedContentUnavailableException : Exception {
    public PublishedContentUnavailableException(string publishedUrl)
        : base($"No published content could be read from {publishedUrl.Quote()}") { }
}
