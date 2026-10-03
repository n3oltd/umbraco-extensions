using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public class RichTextFixResult {
    public int LinksConverted { get; set; }

    public List<string> ButtonsWrapped { get; set; } = new();
    public List<string> EmbedsWrapped { get; set; } = new();
    public List<string> Problems { get; set; } = new();
    public List<string> UnconvertedLinks { get; set; } = new();
}
