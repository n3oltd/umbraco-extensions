using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class ContentmentTextInputDataTypeDesigner : DataTypeDesigner {
    private string _dataSourceKey;
    private IReadOnlyDictionary<string, object> _dataSourceSettings;

    public ContentmentTextInputDataTypeDesigner(IDataTypeService dataTypeService,
                                                IDataTypeContainerService dataTypeContainerService,
                                                PropertyEditorCollection propertyEditors,
                                                IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public ContentmentTextInputDataTypeDesigner DataSource<TDataSource>(IReadOnlyDictionary<string, object> settings)
        where TDataSource : IContentmentDataSource {
        _dataSourceKey = typeof(TDataSource).GetContentmentKey();
        _dataSourceSettings = settings;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        if (!_dataSourceKey.HasValue()) {
            throw new Exception($"Text input {Name.Quote()} has no data source");
        }

        var dataSource = new Dictionary<string, object>();

        dataSource["key"] = _dataSourceKey;
        dataSource["value"] = _dataSourceSettings;

        var configuration = new Dictionary<string, object>();

        configuration["inputType"] = "text";
        configuration["dataSource"] = new[] { dataSource };

        return configuration;
    }

    protected override string EditorAlias => "Umbraco.Community.Contentment.TextInput";

    protected override string EditorUiAlias => "Umb.Contentment.PropertyEditorUi.TextInput";
}
