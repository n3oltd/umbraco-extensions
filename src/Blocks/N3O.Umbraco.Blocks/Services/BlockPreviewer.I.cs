using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks;

public interface IBlockPreviewer {
    Task<string> PreviewBlockAsync(Guid blockId,
                                   IPublishedContent content,
                                   string propertyAlias,
                                   BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData);
}
