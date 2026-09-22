using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class ContentmentListItemsDataTypeDesigner : DataTypeDesigner {
    private bool _confirmRemoval;
    private bool _hideDescription = true;
    private bool _hideIcon = true;
    private int _maxItems;

    public ContentmentListItemsDataTypeDesigner(IDataTypeService dataTypeService,
                                                PropertyEditorCollection propertyEditors,
                                                IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, propertyEditors, configurationEditorJsonSerializer) { }

    public ContentmentListItemsDataTypeDesigner ConfirmRemoval() {
        _confirmRemoval = true;

        return this;
    }

    public ContentmentListItemsDataTypeDesigner Limit(int maxItems) {
        _maxItems = maxItems;

        return this;
    }

    public ContentmentListItemsDataTypeDesigner ShowDescription() {
        _hideDescription = false;

        return this;
    }

    public ContentmentListItemsDataTypeDesigner ShowIcon() {
        _hideIcon = false;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["hideIcon"] = _hideIcon ? "1" : "0";
        configuration["hideDescription"] = _hideDescription ? "1" : "0";
        configuration["confirmRemoval"] = _confirmRemoval ? "1" : "0";
        configuration["maxItems"] = _maxItems;
        configuration["enableDevMode"] = "0";

        return configuration;
    }

    protected override string EditorAlias => "Umbraco.Community.Contentment.ListItems";
}
