using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Blocks;

public class UmbracoBlocksCloner : IBlocksCloner {
    private static readonly string[] EditorAliases = {
        UmbracoConstants.PropertyEditors.Aliases.BlockList,
        UmbracoConstants.PropertyEditors.Aliases.BlockGrid
    };

    private static readonly Regex UdiPattern = new(@"(umb:\/\/\w*\/)(\w*)", RegexOptions.Compiled);

    public bool CanClone(string propertyEditorAlias) {
        return EditorAliases.Any(x => x.EqualsInvariant(propertyEditorAlias));
    }

    public string Clone(string value) {
        var json = ParseObject(value);

        if (json == null) {
            return value;
        }

        var udis = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        TraverseObject(json, udis);

        if (!udis.Any()) {
            return value;
        }

        var keys = new Dictionary<Guid, Guid>();

        // Replacing in the original text leaves everything else byte identical, and reaches udis inside
        // a nested block value, which is held as escaped JSON in one of the outer block's properties
        return UdiPattern.Replace(value, match => {
            if (!udis.Contains(match.Value)) {
                return match.Value;
            }

            var key = Guid.Parse(match.Groups[2].Value);

            if (!keys.ContainsKey(key)) {
                keys[key] = Guid.NewGuid();
            }

            return $"{match.Groups[1]}{keys[key]:N}";
        });
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

    private void ParseUdis(JArray contentData, JArray settingsData, ISet<string> udis) {
        foreach (var item in contentData.Union(settingsData).OfType<JObject>()) {
            var udi = item.SelectToken("$.udi")?.Value<string>();

            if (udi.HasValue()) {
                udis.Add(udi);
            }

            foreach (var property in item.Properties().Where(x => x.Name != "contentTypeKey" && x.Name != "udi")) {
                TraverseProperty(property, udis);
            }
        }
    }

    private void TraverseObject(JObject json, ISet<string> udis) {
        var contentData = json.SelectToken("$.contentData") as JArray;
        var settingsData = json.SelectToken("$.settingsData") as JArray;

        if (contentData != null && settingsData != null) {
            ParseUdis(contentData, settingsData, udis);
        } else {
            foreach (var property in json.Properties()) {
                TraverseProperty(property, udis);
            }
        }
    }

    private void TraverseProperty(JProperty property, ISet<string> udis) {
        if (property.Value is JArray array) {
            foreach (var item in array) {
                TraverseToken(item, udis);
            }
        } else {
            TraverseToken(property.Value, udis);
        }
    }

    private void TraverseToken(JToken token, ISet<string> udis) {
        var json = token as JObject ?? (token is JValue { Value: string text } ? ParseObject(text) : null);

        if (json != null) {
            TraverseObject(json, udis);
        }
    }
}
