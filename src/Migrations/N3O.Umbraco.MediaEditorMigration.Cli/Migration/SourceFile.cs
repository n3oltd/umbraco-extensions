using System.Collections.Generic;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class SourceFile {
    public string Src { get; set; }
    public string MediaId { get; set; }
    public string Filename { get; set; }
    public string Extension { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Bytes { get; set; }
    public string AltText { get; set; }
    public bool IsImage { get; set; }

    public List<CropRect> Crops { get; set; } = new();
}

public sealed class CropRect {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class CropDefinition {
    public string Alias { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class CropOutcome {
    public List<string> WithoutCoordinates { get; } = new();

    public int DroppedRectangles { get; set; }
}
