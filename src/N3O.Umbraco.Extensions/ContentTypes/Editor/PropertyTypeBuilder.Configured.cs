using N3O.Umbraco.DataTypes;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public abstract class ConfiguredPropertyTypeBuilder<TSelf, TDesigner> : PropertyTypeBuilder<TSelf>
    where TSelf : ConfiguredPropertyTypeBuilder<TSelf, TDesigner>
    where TDesigner : IDataTypeDesigner {
    private readonly TDesigner _dataTypeDesigner;

    private Action<TDesigner> _configure;

    protected ConfiguredPropertyTypeBuilder(IDataTypeService dataTypeService, TDesigner dataTypeDesigner)
        : base(dataTypeService) {
        _dataTypeDesigner = dataTypeDesigner;
    }

    public TSelf Configure(Action<TDesigner> configure) {
        _configure = configure;

        return (TSelf) this;
    }

    protected IDataType BuildInlineDataType(PropertyTypeContext context) {
        // An inline data type is identified by the property it serves, so its key is always derived
        _dataTypeDesigner.SetName(context.DataTypeName);
        _dataTypeDesigner.WithoutNameAdoption();
        _dataTypeDesigner.WithDeterministicId(context.DataTypeSeed);

        _configure?.Invoke(_dataTypeDesigner);

        return _dataTypeDesigner.Save();
    }

    protected bool HasConfiguration => _configure != null;
}
