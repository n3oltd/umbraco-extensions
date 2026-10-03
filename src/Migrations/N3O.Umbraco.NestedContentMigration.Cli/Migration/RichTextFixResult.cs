using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public class RichTextFixResult {
    public int EmbedsWrapped { get; set; }
    public int LinksConverted { get; set; }

    public List<string> Problems { get; set; } = new();
    public List<string> UnconvertedLinks { get; set; } = new();
}
