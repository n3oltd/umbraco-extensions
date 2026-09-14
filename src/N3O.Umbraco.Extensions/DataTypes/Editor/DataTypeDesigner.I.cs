using System;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.DataTypes;

public interface IDataTypeDesigner {
    void InFolder(params string[] path);
    IDataType Save();
    void SetName(string name);
    void WithDeterministicId(string seed);
    void WithId(Guid id);
    void WithoutNameAdoption();
}
