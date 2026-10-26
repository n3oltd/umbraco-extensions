using Perplex.ContentBlocks.PropertyEditor;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;

namespace N3O.Umbraco.Blocks.Perplex;

[DataEditor(global::Perplex.ContentBlocks.Constants.PropertyEditor.Alias,
            ValueEditorIsReusable = false,
            ValueType = ValueTypes.Json)]
public class OurContentBlocksPropertyEditor : PerplexContentBlocksPropertyEditor {
    public OurContentBlocksPropertyEditor(IDataValueEditorFactory dataValueEditorFactory,
                                          IIOHelper ioHelper,
                                          IConfigurationEditorJsonSerializer configEditorSerializer,
                                          ContentBlocksPropertyIndexValueFactory propertyIndexValueFactory)
        : base(dataValueEditorFactory, ioHelper, configEditorSerializer, propertyIndexValueFactory) { }

    protected override IDataValueEditor CreateValueEditor() {
        return DataValueEditorFactory.Create<OurContentBlocksValueEditor>(Attribute);
    }
}
