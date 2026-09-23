using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimAttributes {
    private readonly Dictionary<string, List<object>> _values;

    public ScimAttributes() {
        _values = new Dictionary<string, List<object>>(StringComparer.InvariantCultureIgnoreCase);
    }

    public ScimAttributes Add(string path, object value) {
        if (value == null) {
            return this;
        }

        if (!_values.TryGetValue(path, out var existing)) {
            existing = new List<object>();
            _values[path] = existing;
        }

        existing.Add(value);

        return this;
    }

    public bool Has(ScimAttributePath path) {
        return _values.TryGetValue(path.ToString(), out var values) &&
               values.Any(x => x is not string text || text.HasValue());
    }

    public IReadOnlyList<object> Read(ScimAttributePath path) {
        return _values.TryGetValue(path.ToString(), out var values) ? values : [];
    }
}
