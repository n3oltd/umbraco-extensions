namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class RunTotals {
    public int DataTypesConverted { get; set; }
    public int ValuesConverted { get; set; }
    public int ValuesUnchanged { get; set; }
    public int ValuesFailed { get; set; }

    public int MediaNodesCreated { get; set; }

    public int AltTextPreserved { get; set; }

    public int AltTextPropertiesCreated { get; set; }

    public int AltTextDropped { get; set; }

    public int CropsWithoutCoordinates { get; set; }

    public int CropRectanglesDropped { get; set; }

    public bool PublishedCacheInvalidated { get; set; }

    public int NestedValuesConverted { get; set; }
    public int NestedAliasesFixed { get; set; }

    public int LegacyBlockShapesNormalized { get; set; }
}
