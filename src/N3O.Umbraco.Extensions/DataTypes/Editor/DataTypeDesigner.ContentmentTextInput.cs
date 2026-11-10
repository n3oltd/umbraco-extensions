using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class ContentmentTextInputDataTypeDesigner : DataTypeDesigner {
    private IReadOnlyDictionary<string, object> _dataSourceSettings;
    private Type _dataSourceType;

    public ContentmentTextInputDataTypeDesigner(IDataTypeService dataTypeService,
                                                IDataTypeContainerService dataTypeContainerService,
                                                PropertyEditorCollection propertyEditors,
                                                IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public ContentmentTextInputDataTypeDesigner DataSource<TDataSource>(IReadOnlyDictionary<string, object> settings)
        where TDataSource : IContentmentDataSource {
        _dataSourceType = typeof(TDataSource);
        _dataSourceSettings = settings;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["inputType"] = "text";

        if (_dataSourceType.HasValue()) {
            configuration["dataSource"] = BuildContentmentDataSource(_dataSourceType, _dataSourceSettings);
        }

        return configuration;
    }

    protected override string EditorAlias => "Umbraco.Community.Contentment.TextInput";

    protected override string EditorUiAlias => "Umb.Contentment.PropertyEditorUi.TextInput";
}
