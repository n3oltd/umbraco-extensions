using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
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
        return Rewrite(value, udi => new GuidUdi(udi.EntityType, Guid.NewGuid()).ToString());
    }

    public string StripIdentifiers(string value) {
        var index = 0;

        return Rewrite(value, udi => $"{udi.EntityType}/{index++}");
    }

    private string Escape(string udi) {
        return udi.Replace("/", "\\/");
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

    private string Rewrite(string value, Func<GuidUdi, string> getReplacement) {
        var json = ParseObject(value);

        if (json == null) {
            return value;
        }

        var udis = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        TraverseObject(json, udis);

        var ordered = udis.OrderBy(x => value.IndexOf(x, StringComparison.InvariantCultureIgnoreCase)).ToList();

        foreach (var udi in ordered) {
            if (!UdiParser.TryParse(udi, out var parsed) || parsed is not GuidUdi guidUdi) {
                continue;
            }

            var replacement = getReplacement(guidUdi);

            value = value.Replace(udi, replacement, StringComparison.InvariantCultureIgnoreCase);
            value = value.Replace(Escape(udi), Escape(replacement), StringComparison.InvariantCultureIgnoreCase);
        }

        return value;
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
