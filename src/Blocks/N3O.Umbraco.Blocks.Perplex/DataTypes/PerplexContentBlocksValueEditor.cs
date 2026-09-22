using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.ModelValue;
using Perplex.ContentBlocks.Utils;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexContentBlocksValueEditor : ContentBlocksValueEditor {
    private readonly ContentBlocksModelValueDeserializer _deserializer;
    private readonly ILocalizedTextService _localizedTextService;

    public PerplexContentBlocksValueEditor(ContentBlocksModelValueDeserializer deserializer,
                                           ContentBlockUtils utils,
                                           ILocalizedTextService localizedTextService,
                                           IShortStringHelper shortStringHelper,
                                           IJsonSerializer jsonSerializer)
        : base(deserializer, utils, localizedTextService, shortStringHelper, jsonSerializer) {
        _deserializer = deserializer;
        _localizedTextService = localizedTextService;
    }

    public override IValueRequiredValidator RequiredValidator =>
        new PerplexContentBlocksRequiredValidator(_localizedTextService, _deserializer);
}
