using System.Collections.Generic;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class NestedMediaTarget {
    public bool IsCropper { get; set; }
    public List<CropDefinition> CropDefinitions { get; set; } = new();
}
