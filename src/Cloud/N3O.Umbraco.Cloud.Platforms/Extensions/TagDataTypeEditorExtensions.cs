using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class TagDataTypeEditorExtensions {
    public static void SaveTagTextInput(this IDataTypeEditor dataTypeEditor, string name, string tagKey) {
        var settings = new Dictionary<string, object>();
        settings[TagValuesDataSource.TagKeyField] = tagKey;

        var designer = dataTypeEditor.NewContentmentTextInput(name);

        designer.DataSource<TagValuesDataSource>(settings);
        designer.InFolder(Folders.Platforms);
        designer.WithDeterministicId(name);

        designer.Save();
    }
}
