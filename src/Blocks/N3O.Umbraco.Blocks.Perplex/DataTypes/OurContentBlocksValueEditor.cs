using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.Value;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.Blocks.Perplex;

public class OurContentBlocksValueEditor : ContentBlocksValueEditor {
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ContentBlocksValueDeserializer _deserializer;

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
    }

    public override object FromEditor(ContentPropertyData editorValue, object currentValue) {
        var value = base.FromEditor(editorValue, currentValue);

        if (_deserializer.Deserialize(value as string) is not ContentBlocksValue model) {
            return value;
        }

        var changed = false;

        ContentBlocksValueUtils.Iterate(model, block => changed |= ClearSerializedNulls(block.Content));

        return changed ? _jsonSerializer.Serialize(model) : value;
    }

    // Publish validation cannot parse the text "null" that an empty Image Cropper is saved as.
    private bool ClearSerializedNulls(BlockItemData data) {
        var changed = false;

        foreach (var prop in data.Values) {
            if (prop.PropertyType?.PropertyEditorAlias == UmbracoPropertyEditors.Aliases.ImageCropper &&
                prop.Value is "null") {
                prop.Value = null;
                changed = true;
            }
        }

        return changed;
    }
}
