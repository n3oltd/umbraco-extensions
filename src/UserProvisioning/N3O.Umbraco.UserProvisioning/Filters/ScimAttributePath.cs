using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Scim;
using System;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimAttributePath {
    private ScimAttributePath(string schema, string[] elements) {
        Elements = elements;
        Schema = schema;
    }

    public string[] Elements { get; }
    public string Schema { get; }

    public string Attribute => Elements.FirstOrDefault();

    // A path may carry a schema URN, which contains the colons and dots the elements are split on, so the
    // URN is taken off the front before anything is split
    public static ScimAttributePath Parse(string text) {
        if (!text.HasValue()) {
            throw ScimException.InvalidPath("An attribute path cannot be empty");
        }

        string schema = null;
        var remainder = text;
        var lastColon = text.LastIndexOf(':');

        if (lastColon > 0) {
            schema = text.Substring(0, lastColon);
            remainder = text.Substring(lastColon + 1);
        }

        var elements = remainder.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (elements.Length == 0) {
            throw ScimException.InvalidPath($"{text.Quote()} names no attribute");
        }

        return new ScimAttributePath(schema, elements);
    }

    public bool Is(string attribute) {
        return Elements.Length > 0 && Elements[0].Is(attribute);
    }

    public override string ToString() {
        return string.Join(".", Elements);
    }
}
