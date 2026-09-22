using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class NestedContentDataTypeDesigner : DataTypeDesigner {
    private string _elementTypeAlias;
    private int _maxItems;
    private int _minItems;
    private string _nameTemplate;
    private string _tabAlias = "General";

    public NestedContentDataTypeDesigner(IDataTypeService dataTypeService,
                                         PropertyEditorCollection propertyEditors,
                                         IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, propertyEditors, configurationEditorJsonSerializer) { }

    public NestedContentDataTypeDesigner ElementType(string elementTypeAlias) {
        _elementTypeAlias = elementTypeAlias;

        return this;
    }

    public NestedContentDataTypeDesigner ElementType<T>() where T : IUmbracoElement {
        return ElementType(AliasHelper.ContentTypeAlias(typeof(T)));
    }

    public NestedContentDataTypeDesigner Limit(int min, int max) {
        _minItems = min;
        _maxItems = max;

        return this;
    }

    public NestedContentDataTypeDesigner NameTemplate(string nameTemplate) {
        _nameTemplate = nameTemplate;

        return this;
    }

    public NestedContentDataTypeDesigner Tab(string tabAlias) {
        _tabAlias = tabAlias;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        if (!_elementTypeAlias.HasValue()) {
            throw new Exception($"Nested content {Name.Quote()} has no element type");
        }

        var contentType = new NestedContentConfiguration.ContentType();

        contentType.Alias = _elementTypeAlias;
        contentType.TabAlias = _tabAlias;
        contentType.Template = _nameTemplate;

        var configuration = new NestedContentConfiguration();

        configuration.ContentTypes = [contentType];
        configuration.MinItems = _minItems;
        configuration.MaxItems = _maxItems;
        configuration.ConfirmDeletes = true;
        configuration.ShowIcons = false;
        configuration.HideLabel = false;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.NestedContent;
}
