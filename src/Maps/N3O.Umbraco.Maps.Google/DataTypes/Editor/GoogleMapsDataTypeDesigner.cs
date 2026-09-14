using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Maps.DataTypes;

public class GoogleMapsDataTypeDesigner : DataTypeDesigner {
    private string _apiKey;
    private string _location;
    private string _zoom;

    public GoogleMapsDataTypeDesigner(IDataTypeService dataTypeService,
                                      IDataTypeContainerService dataTypeContainerService,
                                      PropertyEditorCollection propertyEditors,
                                      IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public GoogleMapsDataTypeDesigner ApiKey(string apiKey) {
        _apiKey = apiKey;

        return this;
    }

    public GoogleMapsDataTypeDesigner Location(string coordinates) {
        _location = coordinates;

        return this;
    }

    public GoogleMapsDataTypeDesigner Zoom(int zoom) {
        _zoom = zoom.ToString();

        return this;
    }

    // The editor ships as a package manifest, so Umbraco gives it the dictionary configuration editor
    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["apikey"] = _apiKey;

        if (_location.HasValue()) {
            configuration["location"] = _location;
        }

        if (_zoom.HasValue()) {
            configuration["zoom"] = _zoom;
        }

        return configuration;
    }

    protected override string EditorAlias => GoogleMapsConstants.PropertyEditorAlias;
}
