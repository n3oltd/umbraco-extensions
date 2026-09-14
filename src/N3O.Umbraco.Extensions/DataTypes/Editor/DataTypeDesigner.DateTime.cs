using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class DateTimeDataTypeDesigner : DataTypeDesigner {
    public DateTimeDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    protected override object BuildConfiguration(IDataType existing) {
        // v17 removed DateTimeConfiguration.Format/OffsetTime (only TimeZones remains); nothing to configure
        return null;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.DateTime;
}
