using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.ModelValue;
using Perplex.ContentBlocks.Utils;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.Blocks.Perplex;

// GetValueEditor is not virtual on the base, so IDataEditor is re-declared here to remap it onto the
// overloads below.
public class PerplexContentBlocksPropertyEditor : ContentBlocksPropertyEditor, IDataEditor {
    private readonly ContentBlocksModelValueDeserializer _deserializer;
    private readonly ContentBlockUtils _utils;
    private readonly ILocalizedTextService _localizedTextService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IJsonSerializer _jsonSerializer;

    public PerplexContentBlocksPropertyEditor(ContentBlocksModelValueDeserializer deserializer,
                                              ContentBlockUtils utils,
                                              IIOHelper ioHelper,
                                              ILocalizedTextService localizedTextService,
                                              IShortStringHelper shortStringHelper,
                                              IJsonSerializer jsonSerializer,
                                              IPropertyValidationService validationService,
                                              IEditorConfigurationParser editorConfigurationParser)
        : base(deserializer,
               utils,
               ioHelper,
               localizedTextService,
               shortStringHelper,
               jsonSerializer,
               validationService,
               editorConfigurationParser) {
        _deserializer = deserializer;
        _utils = utils;
        _localizedTextService = localizedTextService;
        _shortStringHelper = shortStringHelper;
        _jsonSerializer = jsonSerializer;
    }

    public new IDataValueEditor GetValueEditor() {
        return GetValueEditor(null);
    }

    public new IDataValueEditor GetValueEditor(object configuration) {
        var perplexEditor = (DataValueEditor) base.GetValueEditor(configuration);
        var valueEditor = new PerplexContentBlocksValueEditor(_deserializer,
                                                              _utils,
                                                              _localizedTextService,
                                                              _shortStringHelper,
                                                              _jsonSerializer);

        valueEditor.View = perplexEditor.View;
        valueEditor.Configuration = perplexEditor.Configuration;
        valueEditor.HideLabel = perplexEditor.HideLabel;
        valueEditor.ValueType = perplexEditor.ValueType;

        valueEditor.Validators.AddRange(perplexEditor.Validators);

        return valueEditor;
    }
}
