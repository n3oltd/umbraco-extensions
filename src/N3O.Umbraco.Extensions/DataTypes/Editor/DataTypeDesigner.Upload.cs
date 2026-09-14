using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class UploadDataTypeDesigner : DataTypeDesigner {
    private readonly List<string> _fileExtensions = [];

    public UploadDataTypeDesigner(IDataTypeService dataTypeService,
                                      IDataTypeContainerService dataTypeContainerService,
                                      PropertyEditorCollection propertyEditors,
                                      IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public UploadDataTypeDesigner AllowExtensions(params string[] fileExtensions) {
        _fileExtensions.AddRange(fileExtensions);

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new FileUploadConfiguration();

        configuration.FileExtensions = _fileExtensions.ToList();

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.UploadField;
}
