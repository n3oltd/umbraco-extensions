using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public class LegacyForm {
    public IContent Content { get; set; }
    public string FolderName { get; set; }
    public string Path { get; set; }
    public IReadOnlyList<IContent> Options { get; set; } = [];
}
