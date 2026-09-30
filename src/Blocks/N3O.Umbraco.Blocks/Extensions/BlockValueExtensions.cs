using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockValueExtensions {
    public static BlockEditorData<BlockGridValue, BlockGridLayoutItem> ToEditorData(
        this BlockGridValue blockValue,
        IJsonSerializer jsonSerializer,
        IBlockEditorElementTypeCache elementTypeCache,
        ILogger logger) {
        if (blockValue == null) {
            return null;
        }

        var dataConverter = new BlockGridEditorDataConverter(jsonSerializer);
        var blockEditorValues = new BlockEditorValues<BlockGridValue, BlockGridLayoutItem>(dataConverter,
                                                                                           elementTypeCache,
                                                                                           logger);

        var blockEditorData = blockEditorValues.ConvertAndClean(blockValue);

        if (blockEditorData == null) {
            return null;
        }

        blockEditorData.BlockValue.ContentData.FormatBlockData();
        blockEditorData.BlockValue.SettingsData.FormatBlockData();

        return blockEditorData;
    }
}
