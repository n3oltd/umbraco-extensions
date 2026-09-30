using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.Data.Converters;

public static class UploadDataTypes {
    private static readonly List<string> EditorAliases = [];

    static UploadDataTypes() {
        Register(UmbracoPropertyEditors.Aliases.ImageCropper);
        Register(UmbracoPropertyEditors.Aliases.MediaPicker3);
        Register(UmbracoPropertyEditors.Aliases.UploadField);
    }
    
    public static bool Contains(string editorAlias) {
        return EditorAliases.Any(x => x.EqualsInvariant(editorAlias));
    }
    
    public static void Register(string editorAlias) {
        EditorAliases.Add(editorAlias);
    }
}
