using System.Collections.Generic;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Cropper.DataTypes;

public class CropperConfiguration {
    [ConfigurationField("altText",
                        "Alt Text",
                        "boolean")]
    public bool AltText { get; set; }

    [ConfigurationField("cropDefinitions",
                        "Crop Definitions",
                        "~/App_Plugins/N3O.Umbraco.Cropper/N3O.Umbraco.Cropper.CropDefinitions.html")]
    public IEnumerable<CropDefinition> CropDefinitions { get; set; }

    [ConfigurationField("cropSuggester",
                        "Crop Suggester",
                        "dropdown")]
    public string CropSuggester { get; set; }
}
