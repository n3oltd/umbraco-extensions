using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Scim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimAttributes {
    private readonly Dictionary<string, List<object>> _values;

    public ScimAttributes() {
        _values = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
    }

    // An attribute is registered even when the resource carries no value for it, so a filter naming one
    // the store does not project can be told apart from one that is merely empty
    public ScimAttributes Add(string path, object value) {
        if (!_values.TryGetValue(path, out var existing)) {
            existing = new List<object>();
            _values[path] = existing;
        }

        if (value != null) {
            existing.Add(value);
        }

        return this;
    }

    public bool Has(ScimAttributePath path) {
        return Read(path).Any(x => x is not string text || text.HasValue());
    }

    public IReadOnlyList<object> Read(ScimAttributePath path) {
        if (!_values.TryGetValue(path.ToString(), out var values)) {
            throw ScimException.InvalidFilter($"{path} is not an attribute this endpoint holds");
        }

        return values;
    }
}
