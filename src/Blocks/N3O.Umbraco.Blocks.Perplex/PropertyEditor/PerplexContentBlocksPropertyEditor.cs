using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.Configuration;
using Perplex.ContentBlocks.PropertyEditor.ModelValue;
using Perplex.ContentBlocks.Utils;
using System.Collections.Generic;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexContentBlocksPropertyEditor : IDataEditor {
    private readonly ContentBlocksModelValueDeserializer _deserializer;
    private readonly ContentBlockUtils _utils;
    private readonly IIOHelper _ioHelper;
    private readonly ILocalizedTextService _localizedTextService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IPropertyValidationService _validationService;
    private readonly IEditorConfigurationParser _editorConfigurationParser;

    public PerplexContentBlocksPropertyEditor(ContentBlocksModelValueDeserializer deserializer,
                                              ContentBlockUtils utils,
                                              IIOHelper ioHelper,
                                              ILocalizedTextService localizedTextService,
                                              IShortStringHelper shortStringHelper,
                                              IJsonSerializer jsonSerializer,
                                              IPropertyValidationService validationService,
                                              IEditorConfigurationParser editorConfigurationParser) {
        _deserializer = deserializer;
        _utils = utils;
        _ioHelper = ioHelper;
        _localizedTextService = localizedTextService;
        _shortStringHelper = shortStringHelper;
        _jsonSerializer = jsonSerializer;
        _validationService = validationService;
        _editorConfigurationParser = editorConfigurationParser;
    }

    public string Alias {
        get { return global::Perplex.ContentBlocks.Constants.PropertyEditor.Alias; }
    }

    public EditorType Type {
        get { return EditorType.PropertyValue; }
    }

    public string Name {
        get { return global::Perplex.ContentBlocks.Constants.PropertyEditor.Name; }
    }

    public string Icon {
        get { return "icon-list"; }
    }

    public string Group {
        get { return "Lists"; }
    }

    public bool IsDeprecated {
        get { return false; }
    }

    public IDictionary<string, object> DefaultConfiguration {
        get { return GetConfigurationEditor().DefaultConfiguration; }
    }

    public IPropertyIndexValueFactory PropertyIndexValueFactory {
        get { return new DefaultPropertyIndexValueFactory(); }
    }

    public IConfigurationEditor GetConfigurationEditor() {
        return new ContentBlocksConfigurationEditor(_ioHelper, _editorConfigurationParser);
    }

    public IDataValueEditor GetValueEditor() {
        return GetValueEditor(null);
    }

    public IDataValueEditor GetValueEditor(object configuration) {
        var validator = new ContentBlocksValidator(_deserializer, _utils, _validationService, _shortStringHelper);
        var hideLabel = (configuration as ContentBlocksConfiguration)?.HideLabel ??
                        ContentBlocksConfiguration.DefaultConfiguration.HideLabel;
        var valueEditor = new PerplexContentBlocksValueEditor(_deserializer,
                                                             _utils,
                                                             _localizedTextService,
                                                             _shortStringHelper,
                                                             _jsonSerializer);

        valueEditor.View = global::Perplex.ContentBlocks.Constants.PropertyEditor.ViewPath;
        valueEditor.Configuration = configuration;
        valueEditor.HideLabel = hideLabel;
        valueEditor.ValueType = ValueTypes.Json;
        valueEditor.Validators.Add(validator);

        return valueEditor;
    }
}
