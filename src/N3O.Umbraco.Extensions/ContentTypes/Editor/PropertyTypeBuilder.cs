using Humanizer;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public abstract class PropertyTypeBuilder<TSelf> : IPropertyTypeBuilder where TSelf : PropertyTypeBuilder<TSelf> {
    private readonly IDataTypeService _dataTypeService;

    private string _dataTypeNameOrKey;
    private string _description;
    private bool _labelOnTop;
    private bool _mandatory;
    private string _mandatoryMessage;
    private string _name;
    private string _regex;
    private string _regexMessage;
    private bool _varyByCulture;

    protected PropertyTypeBuilder(IDataTypeService dataTypeService) {
        _dataTypeService = dataTypeService;
    }

    public virtual void Apply(IPropertyType propertyType, PropertyTypeContext context, bool isNew) {
        if (isNew) {
            propertyType.Name = _name ?? context.PropertyAlias.Titleize();
        }

        if (_description.HasValue()) {
            propertyType.Description = _description;
        }

        if (_mandatory) {
            propertyType.Mandatory = true;
            propertyType.MandatoryMessage = _mandatoryMessage;
        }

        if (_regex.HasValue()) {
            propertyType.ValidationRegExp = _regex;
            propertyType.ValidationRegExpMessage = _regexMessage;
        }

        if (_labelOnTop) {
            propertyType.LabelOnTop = true;
        }

        if (_varyByCulture) {
            propertyType.Variations |= ContentVariation.Culture;
        }
    }

    public TSelf DataType(string nameOrKey) {
        _dataTypeNameOrKey = nameOrKey;

        return (TSelf) this;
    }

    public TSelf Description(string description) {
        _description = description;

        return (TSelf) this;
    }

    public TSelf LabelOnTop() {
        _labelOnTop = true;

        return (TSelf) this;
    }

    public TSelf Mandatory(string message = null) {
        _mandatory = true;
        _mandatoryMessage = message;

        return (TSelf) this;
    }

    public TSelf Name(string name) {
        _name = name;

        return (TSelf) this;
    }

    public TSelf Regex(string pattern, string message = null) {
        _regex = pattern;
        _regexMessage = message;

        return (TSelf) this;
    }

    public IDataType ResolveDataType(PropertyTypeContext context) {
        var dataType = default(IDataType);

        if (_dataTypeNameOrKey.HasValue()) {
            if (Guid.TryParse(_dataTypeNameOrKey, out var key)) {
                dataType = _dataTypeService.GetDataType(key);
            } else {
                var deterministicKey = UmbracoId.Deterministic(IdScope.DataType, _dataTypeNameOrKey);

                dataType = _dataTypeService.GetDataType(deterministicKey) ??
                           _dataTypeService.GetDataType(_dataTypeNameOrKey);
            }
        } else {
            dataType = GetDefaultDataType(context);
        }

        if (dataType == null) {
            throw new DataTypeNotFoundException(_dataTypeNameOrKey, context.ContentTypeAlias, context.PropertyAlias);
        }

        return dataType;
    }

    public TSelf VaryByCulture() {
        _varyByCulture = true;

        return (TSelf) this;
    }

    protected abstract IDataType GetDefaultDataType(PropertyTypeContext context);

    protected IDataTypeService DataTypeService => _dataTypeService;
}
