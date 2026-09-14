using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class RichTextDataTypeDesigner : DataTypeDesigner {
    private readonly IContentTypeService _contentTypeService;
    private readonly List<string> _blockElementTypeAliases = [];

    private bool _ignoreUserStartNodes;
    private Guid? _mediaParentKey;

    public RichTextDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    IContentTypeService contentTypeService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) {
        _contentTypeService = contentTypeService;
    }

    public RichTextDataTypeDesigner AllowBlocks(params string[] elementTypeAliases) {
        _blockElementTypeAliases.AddRange(elementTypeAliases);

        return this;
    }

    public RichTextDataTypeDesigner IgnoreUserStartNodes() {
        _ignoreUserStartNodes = true;

        return this;
    }

    public RichTextDataTypeDesigner MediaParent(Guid mediaFolderKey) {
        _mediaParentKey = mediaFolderKey;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new RichTextConfiguration();

        configuration.IgnoreUserStartNodes = _ignoreUserStartNodes;

        if (_mediaParentKey.HasValue()) {
            configuration.MediaParentId = _mediaParentKey.GetValueOrThrow();
        }

        if (!_blockElementTypeAliases.None()) {
            configuration.Blocks = _blockElementTypeAliases.Select(BuildBlock).ToArray();
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.RichText;

    private RichTextConfiguration.RichTextBlockConfiguration BuildBlock(string elementTypeAlias) {
        var block = new RichTextConfiguration.RichTextBlockConfiguration();

        block.ContentElementTypeKey = ResolveElementType(elementTypeAlias).Key;

        return block;
    }

    private IContentType ResolveElementType(string alias) {
        var elementType = _contentTypeService.Get(alias);

        if (elementType == null) {
            throw new Exception($"No element type found with alias {alias.Quote()}");
        }

        return elementType;
    }
}
