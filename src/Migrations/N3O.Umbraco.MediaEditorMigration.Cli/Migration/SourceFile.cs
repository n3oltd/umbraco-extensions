using System.Collections.Generic;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// A file reference parsed from a stored Cropper/Uploader value. Both editors put raw files on
// MediaFileManager.FileSystem at /media/{ticks}/{filename} with NO media node.
public sealed class SourceFile {
    public string Src { get; set; }        // "/media/{ticks}/{filename}" — reused verbatim as the media file path
    public string MediaId { get; set; }    // "{ticks}" — the folder segment of Src
    public string Filename { get; set; }
    public string Extension { get; set; }  // lower-case, with leading dot (".jpg")
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Bytes { get; set; }
    public string AltText { get; set; }
    public bool IsImage { get; set; }

    // Cropper only — the stored crop rectangles, in absolute pixels, positional (index i ⇒ the i-th crop definition).
    public List<CropRect> Crops { get; set; } = new();
}

public sealed class CropRect {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// A crop alias/size defined on the OLD Cropper data type config (cropDefinitions). Positionally aligned with the
// per-value Crops above, and carried onto the native MediaPicker3 data type config + each item's local crops.
public sealed class CropDefinition {
    public string Alias { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// The two ways converting one value's crops can come out imperfect, both of which the caller reports as
// [REVIEW] items. Kept together so neither can be surfaced without the other.
public sealed class CropOutcome {
    // Crop aliases that kept their size but got no coordinates, because the stored value has no source image
    // width/height to convert the pixel rectangle against. The crop falls back to a centre crop / focal point.
    public List<string> WithoutCoordinates { get; } = new();

    // Stored crop rectangles with no crop definition left to name them — crop definitions removed from the data
    // type after the value was saved. There is nowhere to write these, so they are lost.
    public int DroppedRectangles { get; set; }
}
