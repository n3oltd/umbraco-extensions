using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.Value;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;

namespace N3O.Umbraco.Blocks.Perplex;

public class OurContentBlocksValueEditor : ContentBlocksValueEditor {
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ContentBlocksValueDeserializer _deserializer;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IDataTypeConfigurationCache _dataTypeConfigCache;

    public OurContentBlocksValueEditor(IShortStringHelper shortStringHelper,
                                       IJsonSerializer jsonSerializer,
                                       IIOHelper ioHelper,
                                       DataEditorAttribute attribute,
                                       ContentBlocksValidator validator,
                                       ContentBlocksValueDeserializer deserializer,
                                       PropertyEditorCollection propertyEditors,
                                       IDataTypeConfigurationCache dataTypeConfigCache,
                                       DataValueReferenceFactoryCollection referenceFactories)
        : base(shortStringHelper, jsonSerializer, ioHelper, attribute, validator, deserializer, propertyEditors,
               dataTypeConfigCache, referenceFactories) {
        _jsonSerializer = jsonSerializer;
        _deserializer = deserializer;
        _propertyEditors = propertyEditors;
        _dataTypeConfigCache = dataTypeConfigCache;
    }

    public override object FromEditor(ContentPropertyData editorValue, object currentValue) {
        var value = base.FromEditor(editorValue, currentValue);

        if (_deserializer.Deserialize(value as string) is not ContentBlocksValue model) {
            return value;
        }

        var cleared = 0;

        ContentBlocksValueUtils.Iterate(model, block => cleared += ClearSerializedNulls(block.Content));

        return cleared > 0 ? _jsonSerializer.Serialize(model) : value;
    }

    // An empty Image Cropper's FromEditor returns its JSON serialisation, the text "null", which the cropper's own
    // parser rejects when the page is next published.
    private int ClearSerializedNulls(BlockItemData data) {
        if (data == null) {
            return 0;
        }

        var cleared = 0;

        foreach (var prop in data.Values) {
            if (prop.PropertyType == null || prop.Value is not "null") {
                continue;
            }

            var configuration = _dataTypeConfigCache.GetConfiguration(prop.PropertyType.DataTypeKey);
            var valueEditor = _propertyEditors[prop.PropertyType.PropertyEditorAlias]?.GetValueEditor(configuration);

            if (valueEditor?.ValueType == ValueTypes.Json) {
                prop.Value = null;
                cleared++;
            }
        }

        return cleared;
    }
}
