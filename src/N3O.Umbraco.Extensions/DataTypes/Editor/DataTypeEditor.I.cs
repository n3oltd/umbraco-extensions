namespace N3O.Umbraco.DataTypes;

public interface IDataTypeEditor {
    T New<T>(string name) where T : IDataTypeDesigner;
}
