using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyForm {
    public IContent Content { get; set; }
    public string FolderName { get; set; }
    public string Path { get; set; }
    public IReadOnlyList<IContent> Options { get; set; } = [];
}
