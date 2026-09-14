using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoSecurity = Umbraco.Cms.Core.Constants.Security;

namespace N3O.Umbraco.DataTypes;

public abstract class DataTypeDesigner : IDataTypeDesigner {
    private readonly IDataTypeService _dataTypeService;
    private readonly IDataTypeContainerService _dataTypeContainerService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;

    private bool _adoptByName = true;
    private string[] _folderPath = [];
    private Guid? _id;
    private string _name;

    protected DataTypeDesigner(IDataTypeService dataTypeService,
                               IDataTypeContainerService dataTypeContainerService,
                               PropertyEditorCollection propertyEditors,
                               IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) {
        _dataTypeService = dataTypeService;
        _dataTypeContainerService = dataTypeContainerService;
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
            dataType.ConfigurationData = ResolveEditor().GetConfigurationEditor()
                                                        .FromConfigurationObject(configuration,
                                                                                 _configurationEditorJsonSerializer);
        }

        var save = existing != null
                       ? _dataTypeService.UpdateAsync(dataType, UmbracoSecurity.SuperUserKey)
                       : _dataTypeService.CreateAsync(dataType, UmbracoSecurity.SuperUserKey);
        var attempt = save.GetAwaiter().GetResult();

        if (!attempt.Success) {
            throw new Exception($"Failed to save data type {_name.Quote()}: {attempt.Status}");
        }

        return attempt.Result;
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
            dataType = _dataTypeService.GetAsync(_id.GetValueOrThrow()).GetAwaiter().GetResult();
        }

        if (dataType == null && _adoptByName) {
            dataType = _dataTypeService.GetAsync(_name).GetAwaiter().GetResult();
        }

        return dataType;
    }

    private int GetOrCreateFolder() {
        var container = default(EntityContainer);
        var walkedPath = new List<string>();

        foreach (var element in _folderPath) {
            walkedPath.Add(element);

            var elementContainer = default(EntityContainer);

            var level = container == null ? 1 : container.Level + 1;
            var containers = _dataTypeContainerService.GetAsync(element, level).GetAwaiter().GetResult();

            if (container == null) {
                elementContainer = containers.SingleOrDefault();
            } else {
                elementContainer = containers.SingleOrDefault(x => x.ParentId == container.Id);
            }

            if (elementContainer == null) {
                var key = UmbracoId.Deterministic(IdScope.DataTypeFolder, walkedPath.ToArray());
                var attempt = _dataTypeContainerService
                                  .CreateAsync(key, element, container?.Key, UmbracoSecurity.SuperUserKey)
                                  .GetAwaiter()
                                  .GetResult();

                if (!attempt.Success) {
                    throw new Exception($"Failed to create data type folder {element.Quote()}: {attempt.Status}");
                }

                container = attempt.Result;
            } else {
                container = elementContainer;
            }
        }

        return container?.Id ?? -1;
    }
}
