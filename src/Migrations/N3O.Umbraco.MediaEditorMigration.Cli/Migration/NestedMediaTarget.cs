using System.Collections.Generic;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// A single element-type property that was backed by the Cropper or Uploader editor, captured BEFORE the data
// types are flipped to Umbraco.MediaPicker3. Keyed by (element content-type key, property alias) so a value
// nested inside a Block List / Block Grid / Perplex block can be matched to its crop definitions.
public sealed class NestedMediaTarget {
    public bool IsCropper { get; set; }
    public List<CropDefinition> CropDefinitions { get; set; } = new();
}
