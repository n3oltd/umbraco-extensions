using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyForm {
    public IContent Content { get; set; }
    public string FolderName { get; set; }
    public string Path { get; set; }
    public IReadOnlyList<IContent> Options { get; set; } = [];

    // A site can nest a second grouping layer that is itself a form type, so a form is not always the whole of what
    // a page picking it used to show. Each nested form migrates to its own campaign, which the picker cannot hold
    // alongside this one.
    public IReadOnlyList<IContent> NestedForms { get; set; } = [];
}
