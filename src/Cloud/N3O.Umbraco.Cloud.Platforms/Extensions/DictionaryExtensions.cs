using N3O.Umbraco.Cloud.Platforms.Clients;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DictionaryExtensions {
    public static TagCollectionReq ToTagCollectionReq(this IReadOnlyDictionary<string, string> tags) {
        if (tags == null) {
            return null;
        }
        
        var entries = new List<TagReq>();
        
        foreach (var tag in tags) {
            var entry = new TagReq();
            entry.Key = tag.Key;
            entry.Value = tag.Value;

            entries.Add(entry);
        }
        
        var req = new TagCollectionReq();
        req.Entries = entries;

        return req;
    }
}