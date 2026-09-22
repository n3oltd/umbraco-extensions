using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public abstract class DataTypeDesigner : IDataTypeDesigner {
    private readonly IDataTypeService _dataTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;

    private bool _adoptByName = true;
    private string[] _folderPath = [];
    private Guid? _id;
    private string _name;

    protected DataTypeDesigner(IDataTypeService dataTypeService,
                               PropertyEditorCollection propertyEditors,
                               IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) {
        _dataTypeService = dataTypeService;
        _propertyEditors = propertyEditors;
        _configurationEditorJsonSerializer = configurationEditorJsonSerializer;
    }

    public void InFolder(params string[] path) {
        _folderPath = path;
    }

    public IDataType Save() {
        var dataType = FindExisting() ?? Create();

        if (!dataType.EditorAlias.EqualsInvariant(EditorAlias)) {
            throw new Exception($"Data type {_name.Quote()} uses editor {dataType.EditorAlias.Quote()} so cannot " +
                                $"be converged to editor {EditorAlias.Quote()}");
        }

        var existing = dataType.Id > 0 ? dataType : null;
        var configuration = BuildConfiguration(existing);

        dataType.Name = _name;

        // Only the backoffice save path derives this, so without it every value would be stored as Ntext
        dataType.DatabaseType = ValueTypes.ToStorageType(ResolveEditor().GetValueEditor().ValueType);

        // Assigning null throws, so designers with nothing to configure keep the editor default
        if (configuration != null) {
            dataType.Configuration = configuration;
        }

        _dataTypeService.Save(dataType);

        return dataType;
    }

    public void SetName(string name) {
        _name = name;
    }

    public void WithDeterministicId(string seed) {
        _id = UmbracoId.Deterministic(IdScope.DataType, seed);
    }

    public void WithId(Guid id) {
        _id = id;
    }

    public void WithoutNameAdoption() {
        _adoptByName = false;
    }

    protected abstract object BuildConfiguration(IDataType existing);

    protected abstract string EditorAlias { get; }

    protected string Name => _name;

    private IDataType Create() {
        var parentId = GetOrCreateFolder();
        var dataType = new DataType(ResolveEditor(), _configurationEditorJsonSerializer, parentId);

        if (_id.HasValue()) {
            dataType.Key = _id.GetValueOrThrow();
        }

        return dataType;
    }

    private IDataEditor ResolveEditor() {
        if (!_propertyEditors.TryGet(EditorAlias, out var editor)) {
            throw new Exception($"Property editor {EditorAlias.Quote()} not found");
        }

        return editor;
    }

    private IDataType FindExisting() {
        var dataType = default(IDataType);

        if (_id.HasValue()) {
            dataType = _dataTypeService.GetDataType(_id.GetValueOrThrow());
        }

        if (dataType == null && _adoptByName) {
            dataType = _dataTypeService.GetDataType(_name);
        }

        return dataType;
    }

    private int GetOrCreateFolder() {
        var container = default(EntityContainer);
        var walkedPath = new List<string>();

        foreach (var element in _folderPath) {
            walkedPath.Add(element);

            var elementContainer = default(EntityContainer);

            if (container == null) {
                elementContainer = _dataTypeService.GetContainers(element, 1).SingleOrDefault();
            } else {
                elementContainer = _dataTypeService.GetContainers(element, container.Level + 1)
                                                   .SingleOrDefault(x => x.ParentId == container.Id);
            }

            if (elementContainer == null) {
                var key = UmbracoId.Deterministic(IdScope.DataTypeFolder, walkedPath.ToArray());
                var attempt = _dataTypeService.CreateContainer(container?.Id ?? -1, key, element);

                if (!attempt.Success) {
                    throw new Exception($"Failed to create data type folder {element.Quote()}", attempt.Exception);
                }

                container = attempt.Result.Entity;
            } else {
                container = elementContainer;
            }
        }

        return container?.Id ?? -1;
    }
}
