using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.DataTypes;

public interface IDataTypeEditor {
    IDataType Find(string name);
    T New<T>(string name) where T : IDataTypeDesigner;
}
