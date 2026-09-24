using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Json;

public static class ScimProjection {
    private static readonly string[] Always = ["id", "schemas"];

    public static object Apply(object resource, IReadOnlyList<string> attributes, IReadOnlyList<string> excluded) {
        if (!attributes.OrEmpty().Any() && !excluded.OrEmpty().Any()) {
            return resource;
        }

        if (attributes.OrEmpty().Any() && excluded.OrEmpty().Any()) {
            throw ScimException.InvalidValue("A request cannot both name attributes and exclude them");
        }

        var json = JToken.Parse(ScimJson.Write(resource));

        foreach (var member in Resources(json)) {
            Project(member, attributes, excluded);
        }

        return json;
    }

    public static IReadOnlyList<string> Read(string value) {
        if (!value.HasValue()) {
            return [];
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => x.HasValue())
                    .ToList();
    }

    private static JProperty Find(JObject json, string name) {
        return json.Properties().FirstOrDefault(x => x.Name.Is(name));
    }

    // A path names an attribute and optionally one sub-attribute, and may carry the schema URN it
    // belongs to, which is the part before the last colon
    private static IReadOnlyList<string> Parse(string path) {
        var qualified = path.LastIndexOf(':');

        return (qualified >= 0 ? path.Substring(qualified + 1) : path).Split('.');
    }

    private static void Project(JToken resource, IReadOnlyList<string> attributes, IReadOnlyList<string> excluded) {
        if (resource is not JObject json) {
            return;
        }

        if (excluded.OrEmpty().Any()) {
            foreach (var path in excluded.Where(x => !Always.Any(a => Parse(x).First().Is(a)))) {
                Remove(json, Parse(path));
            }
        } else {
            var kept = attributes.Select(Parse).Concat(Always.Select(x => (IReadOnlyList<string>) [x])).ToList();

            foreach (var property in json.Properties().ToList()) {
                var named = kept.Where(x => x.First().Is(property.Name)).ToList();

                if (!named.Any()) {
                    property.Remove();
                } else if (named.All(x => x.Count > 1)) {
                    Retain(property.Value, named.Select(x => x.Skip(1).ToList()).ToList());
                }
            }
        }
    }

    private static void Remove(JObject json, IReadOnlyList<string> path) {
        var property = Find(json, path.First());

        if (property == null) {
            return;
        }

        if (path.Count == 1) {
            property.Remove();
        } else {
            foreach (var child in Values(property.Value)) {
                Remove(child, path.Skip(1).ToList());
            }
        }
    }

    private static IEnumerable<JToken> Resources(JToken json) {
        var listed = json is JObject envelope ? envelope.Properties().FirstOrDefault(x => x.Name.Is("Resources")) : null;

        return listed?.Value as JArray ?? (IEnumerable<JToken>) new[] { json };
    }

    private static void Retain(JToken value, IReadOnlyList<IReadOnlyList<string>> paths) {
        foreach (var child in Values(value)) {
            foreach (var property in child.Properties().ToList()) {
                if (!paths.Any(x => x.First().Is(property.Name))) {
                    property.Remove();
                }
            }
        }
    }

    private static IEnumerable<JObject> Values(JToken value) {
        return value is JArray array ? array.OfType<JObject>() : new[] { value as JObject }.OfType<JObject>();
    }
}
