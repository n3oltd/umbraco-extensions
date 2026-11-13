using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Redirects;

public static class StaticRedirects {
    private static readonly Dictionary<string, Redirect> Redirects = new(StringComparer.InvariantCultureIgnoreCase);

    public static void Add(string oldPath, string newPath, bool temporary = false) {
        Redirects[GetKey(oldPath)] = new Redirect($"/{Normalize(newPath)}", temporary);
    }

    public static void Clear() {
        Redirects.Clear();
    }
    
    public static Redirect Find(string path) {
        return Redirects.GetValueOrDefault(GetKey(path));
    }

    private static string GetKey(string path) {
        return Uri.UnescapeDataString(Normalize(path ?? string.Empty));
    }
    
    private static string Normalize(string path) {
        return path.TrimStart('/').TrimEnd('/');
    }
}
