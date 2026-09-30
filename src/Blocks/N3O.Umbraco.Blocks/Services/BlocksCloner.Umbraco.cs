using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Blocks;

public class UmbracoBlocksCloner : IBlocksCloner {
    private static readonly string[] EditorAliases = {
        UmbracoConstants.PropertyEditors.Aliases.BlockList,
        UmbracoConstants.PropertyEditors.Aliases.BlockGrid
    };

    public bool CanClone(string propertyEditorAlias) {
        return EditorAliases.Any(x => x.EqualsInvariant(propertyEditorAlias));
    }

    public string Clone(string value) {
        return Rewrite(value, () => Guid.NewGuid().ToString("D"));
    }

    public bool IsEmpty(string value) {
        var contentData = ParseObject(value)?.SelectToken("$.contentData") as JArray;

        return contentData != null && contentData.None();
    }

    public string StripIdentifiers(string value) {
        var index = 0;

        return Rewrite(value?.Replace("%", "%%", StringComparison.InvariantCultureIgnoreCase),
                       () => $"%key/{index++}%");
    }

    private JObject ParseObject(string json) {
        if (!json.HasValue() || !json.DetectIsJson()) {
            return null;
        }

        try {
            return JObject.Parse(json);
        } catch (JsonException) {
            return null;
        }
    }

    private void ParseKeys(JArray contentData, JArray settingsData, ISet<string> keys) {
        foreach (var item in contentData.Union(settingsData).OfType<JObject>()) {
            var key = item.SelectToken("$.key")?.Value<string>();

            if (key.HasValue()) {
                keys.Add(key);
            }

            foreach (var property in item.Properties().Where(x => x.Name != "contentTypeKey" && x.Name != "key")) {
                TraverseProperty(property, keys);
            }
        }
    }

    private string Rewrite(string value, Func<string> getReplacement) {
        var json = ParseObject(value);

        if (json == null) {
            return value;
        }

        var keys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        TraverseObject(json, keys);

        var ordered = keys.OrderBy(x => value.IndexOf(x, StringComparison.InvariantCultureIgnoreCase)).ToList();

        foreach (var key in ordered) {
            if (!Guid.TryParse(key, out _)) {
                continue;
            }

            value = value.Replace(key, getReplacement(), StringComparison.InvariantCultureIgnoreCase);
        }

        return value;
    }

    private void TraverseObject(JObject json, ISet<string> keys) {
        var contentData = json.SelectToken("$.contentData") as JArray;
        var settingsData = json.SelectToken("$.settingsData") as JArray;

        if (contentData != null && settingsData != null) {
            ParseKeys(contentData, settingsData, keys);
        } else {
            foreach (var property in json.Properties()) {
                TraverseProperty(property, keys);
            }
        }
    }

    private void TraverseProperty(JProperty property, ISet<string> keys) {
        if (property.Value is JArray array) {
            foreach (var item in array) {
                TraverseToken(item, keys);
            }
        } else {
            TraverseToken(property.Value, keys);
        }
    }

    private void TraverseToken(JToken token, ISet<string> keys) {
        var json = token as JObject ?? (token is JValue { Value: string text } ? ParseObject(text) : null);

        if (json != null) {
            TraverseObject(json, keys);
        }
    }
}
