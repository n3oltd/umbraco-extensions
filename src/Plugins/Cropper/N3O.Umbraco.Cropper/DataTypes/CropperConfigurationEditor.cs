using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cropper.DataTypes;

public class CropperConfigurationEditor : ConfigurationEditor<CropperConfiguration> {
    public CropperConfigurationEditor(IIOHelper ioHelper,
                                      IEditorConfigurationParser editorConfigurationParser,
                                      IEnumerable<ICropSuggester> cropSuggesters)
        : base(ioHelper, editorConfigurationParser) {
        var prevalues = cropSuggesters.Select(x => new { value = x.Alias, label = x.Alias })
                                      .OrderBy(x => x.label)
                                      .Prepend(new { value = "", label = "None" })
                                      .ToList();

        Field(nameof(CropperConfiguration.CropSuggester)).Config["prevalues"] = prevalues;
    }
}
