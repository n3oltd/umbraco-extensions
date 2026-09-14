using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class TagsDataTypeDesigner : DataTypeDesigner {
    private string _group = "default";

    public TagsDataTypeDesigner(IDataTypeService dataTypeService,
                                IDataTypeContainerService dataTypeContainerService,
                                PropertyEditorCollection propertyEditors,
                                IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public TagsDataTypeDesigner Group(string group) {
        _group = group;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new TagConfiguration();

        configuration.Group = _group;
        configuration.StorageType = TagsStorageType.Json;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.Tags;
}
