using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockValueExtensions {
    // Mutates blockValue: the converter backfills keys, clears the legacy raw values and rewrites Expose.
    public static BlockEditorData<BlockGridValue, BlockGridLayoutItem> ToEditorData(
        this BlockGridValue blockValue,
        IJsonSerializer jsonSerializer,
        IContentTypeService contentTypeService) {
        if (blockValue == null) {
            return null;
        }

        var blockEditorData = new BlockGridEditorDataConverter(jsonSerializer).Convert(blockValue);

        return Clean(blockEditorData, contentTypeService);
    }

    private static BlockEditorData<BlockGridValue, BlockGridLayoutItem> Clean(
        BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData,
        IContentTypeService contentTypeService) {
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

        Resolve(blockEditorData.BlockValue.ContentData, contentKeys, contentTypes, propertyTypes);
        Resolve(blockEditorData.BlockValue.SettingsData, settingsKeys, contentTypes, propertyTypes);

        blockEditorData.BlockValue.ContentData.RemoveAll(x => !x.ContentTypeAlias.HasValue());
        blockEditorData.BlockValue.SettingsData.RemoveAll(x => !x.ContentTypeAlias.HasValue());

        // Must follow the resolve above, which is what supplies the property types formatting reads.
        blockEditorData.BlockValue.ContentData.FormatBlockData();
        blockEditorData.BlockValue.SettingsData.FormatBlockData();

        return blockEditorData;
    }

    private static void Resolve(List<BlockItemData> blocks,
                                HashSet<Guid> referencedKeys,
                                IReadOnlyDictionary<Guid, IContentType> contentTypes,
                                IDictionary<Guid, Dictionary<string, IPropertyType>> propertyTypes) {
        foreach (var block in blocks.Where(x => x.Key != Guid.Empty && referencedKeys.Contains(x.Key))) {
            ResolveBlockItemData(block, contentTypes, propertyTypes);
        }
    }

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
                var blockPropertyValue = new BlockPropertyValue();
                blockPropertyValue.Alias = value.Alias;
                blockPropertyValue.Value = value.Value;
                blockPropertyValue.PropertyType = propertyType;

                block.Values.Add(blockPropertyValue);
            }
        }

        block.ContentTypeAlias = contentType.Alias;
    }
}
