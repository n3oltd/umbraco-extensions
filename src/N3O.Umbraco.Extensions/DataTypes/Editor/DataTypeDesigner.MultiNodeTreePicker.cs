using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class MultiNodeTreePickerDataTypeDesigner : DataTypeDesigner {
    private readonly IContentTypeService _contentTypeService;
    private readonly List<string> _contentTypeAliases = [];

    private int _maxNumber;
    private int _minNumber;

    public MultiNodeTreePickerDataTypeDesigner(IDataTypeService dataTypeService,
                                               IDataTypeContainerService dataTypeContainerService,
                                               IContentTypeService contentTypeService,
                                               PropertyEditorCollection propertyEditors,
                                               IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) {
        _contentTypeService = contentTypeService;
    }

    public MultiNodeTreePickerDataTypeDesigner AllowContentTypes(params string[] contentTypeAliases) {
        _contentTypeAliases.AddRange(contentTypeAliases);

        return this;
    }

    public MultiNodeTreePickerDataTypeDesigner Limit(int min, int max) {
        _minNumber = min;
        _maxNumber = max;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new MultiNodePickerConfiguration();

        configuration.MaxNumber = _maxNumber;
        configuration.MinNumber = _minNumber;

        if (!_contentTypeAliases.None()) {
            configuration.Filter = _contentTypeAliases.Select(x => _contentTypeService.GetOrThrow(x).Key).ToCsv();
        }

        return configuration;
    }

    protected override string EditorAlias =>
        UmbracoPropertyEditors.Aliases.MultiNodeTreePicker;
}
