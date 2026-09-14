using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

using GridEditorData =
    Umbraco.Cms.Core.Models.Blocks.BlockEditorData<Umbraco.Cms.Core.Models.Blocks.BlockGridValue,
                                                  Umbraco.Cms.Core.Models.Blocks.BlockGridLayoutItem>;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockValueExtensions {
    // Converting the value walks the layout to collect the content and settings references, which is what
    // identifies each block. Cleaning then attaches the property types those references point at.
    public static GridEditorData ToEditorData(this BlockGridValue blockValue,
                                              IJsonSerializer jsonSerializer,
                                              IContentTypeService contentTypeService) {
        if (blockValue == null) {
            return null;
        }

        var blockEditorData = new BlockGridEditorDataConverter(jsonSerializer).Convert(blockValue);

        return Clean(blockEditorData, contentTypeService);
    }

    private static GridEditorData Clean(GridEditorData blockEditorData, IContentTypeService contentTypeService) {
        if (blockEditorData.BlockValue.ContentData.Count == 0) {
            blockEditorData.BlockValue.SettingsData.Clear();

            return null;
        }

        var contentKeys = blockEditorData.References.Select(x => x.ContentKey).ToHashSet();
        var settingsKeys = blockEditorData.References.Where(x => x.SettingsKey.HasValue)
                                          .Select(x => x.SettingsKey.Value)
                                          .ToHashSet();

        var contentTypeKeys = blockEditorData.BlockValue
                                             .ContentData
                                             .Concat(blockEditorData.BlockValue.SettingsData)
                                             .Select(x => x.ContentTypeKey)
                                             .Distinct();

        var contentTypes = contentTypeService.GetMany(contentTypeKeys).ToDictionary(x => x.Key);
        var propertyTypes = new Dictionary<Guid, Dictionary<string, IPropertyType>>();

        Resolve(blockEditorData.BlockValue.ContentData, contentKeys);
        Resolve(blockEditorData.BlockValue.SettingsData, settingsKeys);

        blockEditorData.BlockValue.ContentData.RemoveAll(x => !x.ContentTypeAlias.HasValue());
        blockEditorData.BlockValue.SettingsData.RemoveAll(x => !x.ContentTypeAlias.HasValue());

        // Formatting is done here rather than left to the caller because it needs the property types resolved
        // above, and it applies to settings exactly as it does to content: a settings element renders through
        // the same value converters.
        blockEditorData.BlockValue.ContentData.FormatBlockData();
        blockEditorData.BlockValue.SettingsData.FormatBlockData();

        return blockEditorData;

        void Resolve(List<BlockItemData> blocks, HashSet<Guid> referencedKeys) {
            foreach (var block in blocks.Where(x => x.Key != Guid.Empty && referencedKeys.Contains(x.Key))) {
                ResolveBlockItemData(block, contentTypes, propertyTypes);
            }
        }
    }

    // A block's values arrive keyed by alias only. Each is matched to its property type so that downstream value
    // converters have the data type to convert against, and any value whose property no longer exists is dropped.
    private static void ResolveBlockItemData(BlockItemData block,
                                             IReadOnlyDictionary<Guid, IContentType> contentTypes,
                                             IDictionary<Guid, Dictionary<string, IPropertyType>> propertyTypes) {
        if (!contentTypes.TryGetValue(block.ContentTypeKey, out var contentType)) {
            return;
        }

        if (!propertyTypes.TryGetValue(contentType.Key, out var contentTypePropertyTypes)) {
            contentTypePropertyTypes = propertyTypes[contentType.Key] = contentType.CompositionPropertyTypes
                                                                                  .ToDictionary(x => x.Alias);
        }

        var sourceValues = block.Values.ToList();

        block.Values.Clear();

        foreach (var value in sourceValues) {
            if (contentTypePropertyTypes.TryGetValue(value.Alias, out var propertyType)) {
                block.Values.Add(new BlockPropertyValue {
                    Alias = value.Alias,
                    Value = value.Value,
                    PropertyType = propertyType
                });
            }
        }

        block.ContentTypeAlias = contentType.Alias;
    }
}
