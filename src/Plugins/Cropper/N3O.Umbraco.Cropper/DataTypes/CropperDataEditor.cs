using System.Collections.Generic;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cropper.DataTypes;

[DataEditor(CropperConstants.PropertyEditorAlias,
            "N3O Cropper",
            "~/App_Plugins/N3O.Umbraco.Cropper/N3O.Umbraco.Cropper.html",
            HideLabel = false,
            ValueType = ValueTypes.Json)]
public class CropperDataEditor : DataEditor {
    private readonly IIOHelper _ioHelper;
    private readonly IEditorConfigurationParser _editorConfigurationParser;
    private readonly IEnumerable<ICropSuggester> _cropSuggesters;

    public CropperDataEditor(IDataValueEditorFactory dataValueEditorFactory,
                             IIOHelper ioHelper,
                             IEditorConfigurationParser editorConfigurationParser,
                             IEnumerable<ICropSuggester> cropSuggesters)
        : base(dataValueEditorFactory) {
        _ioHelper = ioHelper;
        _editorConfigurationParser = editorConfigurationParser;
        _cropSuggesters = cropSuggesters;
    }

    protected override IConfigurationEditor CreateConfigurationEditor() {
        return new CropperConfigurationEditor(_ioHelper, _editorConfigurationParser, _cropSuggesters);
    }
}
