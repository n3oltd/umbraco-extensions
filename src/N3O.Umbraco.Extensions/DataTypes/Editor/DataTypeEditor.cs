using Microsoft.Extensions.DependencyInjection;
using System;

namespace N3O.Umbraco.DataTypes;

public class DataTypeEditor : IDataTypeEditor {
    private readonly IServiceProvider _serviceProvider;

    public DataTypeEditor(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    public T New<T>(string name) where T : IDataTypeDesigner {
        var designer = _serviceProvider.GetRequiredService<T>();

        designer.SetName(name);

        return designer;
    }
}
