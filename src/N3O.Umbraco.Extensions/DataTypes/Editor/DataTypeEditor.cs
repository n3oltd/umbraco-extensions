using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Utilities;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class DataTypeEditor : IDataTypeEditor {
    private readonly IDataTypeService _dataTypeService;
    private readonly IServiceProvider _serviceProvider;

    public DataTypeEditor(IDataTypeService dataTypeService, IServiceProvider serviceProvider) {
        _dataTypeService = dataTypeService;
        _serviceProvider = serviceProvider;
    }

    public IDataType Find(string name) {
        return _dataTypeService.GetDataType(UmbracoId.Deterministic(IdScope.DataType, name)) ??
               _dataTypeService.GetDataType(name);
    }

    public T New<T>(string name) where T : IDataTypeDesigner {
        var designer = _serviceProvider.GetRequiredService<T>();

        designer.SetName(name);

        return designer;
    }
}
