using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks;

public interface IBlockPreviewer {
    // The content is the page the block is being previewed on. It is passed rather than just its type alias
    // because converting the grid value needs it as the property's owner: from v15 a block value carries expose
    // entries, and aligning those reads owner.ContentType.Variations. The property alias comes from the editor
    // rather than being assumed, as a document type can hold more than one block grid under different names.
    Task<string> PreviewBlockAsync(Guid blockId,
                                   IPublishedContent content,
                                   string propertyAlias,
                                   BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData);
}
