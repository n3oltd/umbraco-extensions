using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
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
        if (!value.HasValue()) {
            return value;
        }

        var blockValue = JsonConvert.DeserializeObject<BlockValue>(value);
        var replacements = GetReplacements(blockValue);

        if (!replacements.Any()) {
            return value;
        }

        var json = JObject.Parse(value);

        // The layout references these udis, so every occurrence is replaced, not just the two data lists
        foreach (var token in json.Descendants().OfType<JValue>().ToList()) {
            if (token.Type == JTokenType.String &&
                replacements.TryGetValue((string) token.Value, out var replacement)) {
                token.Value = replacement;
            }
        }

        return json.ToString(Formatting.None);
    }

    private Dictionary<string, string> GetReplacements(BlockValue blockValue) {
        var replacements = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (blockValue == null) {
            return replacements;
        }

        var items = (blockValue.ContentData ?? new List<BlockItemData>())
            .Concat(blockValue.SettingsData ?? new List<BlockItemData>());

        foreach (var item in items) {
            if (item?.Udi is GuidUdi udi && !replacements.ContainsKey(udi.ToString())) {
                replacements[udi.ToString()] = new GuidUdi(udi.EntityType, Guid.NewGuid()).ToString();
            }
        }

        return replacements;
    }
}
