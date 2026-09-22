using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class ContentmentDataListDataTypeDesigner : DataTypeDesigner {
    private const string ItemPickerEditor =
        "Umbraco.Community.Contentment.DataEditors.ItemPickerDataListEditor, Umbraco.Community.Contentment";

    private bool _allowClear;
    private string _dataSourceKey;
    private bool _enableMultiple;
    private int _maxItems;

    public ContentmentDataListDataTypeDesigner(IDataTypeService dataTypeService,
                                               PropertyEditorCollection propertyEditors,
                                               IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, propertyEditors, configurationEditorJsonSerializer) { }

    public ContentmentDataListDataTypeDesigner AllowClear() {
        _allowClear = true;

        return this;
    }

    public ContentmentDataListDataTypeDesigner AllowMultiple() {
        _enableMultiple = true;

        return this;
    }

    public ContentmentDataListDataTypeDesigner DataSource<TDataSource>() where TDataSource : IContentmentDataSource {
        return DataSource(typeof(TDataSource));
    }

    // Contentment stores the source as an assembly-qualified name, so it must survive a rename of the type
    public ContentmentDataListDataTypeDesigner DataSource(Type dataSourceType) {
        _dataSourceKey = $"{dataSourceType.FullName}, {dataSourceType.Assembly.GetName().Name}";

        return this;
    }

    public ContentmentDataListDataTypeDesigner Limit(int maxItems) {
        _maxItems = maxItems;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        if (!_dataSourceKey.HasValue()) {
            throw new Exception($"Data list {Name.Quote()} has no data source");
        }

        var listEditorValue = new Dictionary<string, object>();

        listEditorValue["overlaySize"] = "small";
        listEditorValue["listType"] = "list";
        listEditorValue["defaultIcon"] = "icon-document";
        listEditorValue["enableFilter"] = "1";
        listEditorValue["maxItems"] = _maxItems;
        listEditorValue["allowClear"] = _allowClear ? "1" : "0";

        if (_enableMultiple) {
            listEditorValue["enableMultiple"] = "1";
        }

        var dataSource = new Dictionary<string, object>();

        dataSource["key"] = _dataSourceKey;
        dataSource["value"] = new Dictionary<string, object>();

        var listEditor = new Dictionary<string, object>();

        listEditor["key"] = ItemPickerEditor;
        listEditor["value"] = listEditorValue;

        var configuration = new Dictionary<string, object>();

        configuration["dataSource"] = new[] { dataSource };
        configuration["listEditor"] = new[] { listEditor };
        configuration["preview"] = null;

        return configuration;
    }

    protected override string EditorAlias => "Umbraco.Community.Contentment.DataList";
}
