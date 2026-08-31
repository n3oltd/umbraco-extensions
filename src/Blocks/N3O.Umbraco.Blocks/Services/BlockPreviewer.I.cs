using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks;

public interface IBlockPreviewer {
    // content is the page being previewed on, needed as the property owner because aligning a block value's
    // expose entries reads owner.ContentType.Variations from v15. blockEditorData is converted once per
    // document and shared across every block on it, so an implementation must treat it as read only.
    Task<string> PreviewBlockAsync(Guid blockId,
                                   IPublishedContent content,
                                   string propertyAlias,
                                   BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData);
}
