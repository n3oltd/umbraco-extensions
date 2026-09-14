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

public class BlockListDataTypeDesigner : DataTypeDesigner {
    private readonly IContentTypeService _contentTypeService;
    private readonly List<BlockListBlockBuilder> _blocks = [];

    private int? _maxBlocks;
    private int? _minBlocks;
    private bool _useSingleBlockMode;

    public BlockListDataTypeDesigner(IDataTypeService dataTypeService,
                                     IDataTypeContainerService dataTypeContainerService,
                                     IContentTypeService contentTypeService,
                                     PropertyEditorCollection propertyEditors,
                                     IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) {
        _contentTypeService = contentTypeService;
    }

    public BlockListBlockBuilder AddBlock(string elementTypeAlias) {
        var block = new BlockListBlockBuilder(elementTypeAlias);

        _blocks.Add(block);

        return block;
    }

    public BlockListDataTypeDesigner AllowBlocks(params string[] elementTypeAliases) {
        foreach (var alias in elementTypeAliases) {
            AddBlock(alias);
        }

        return this;
    }

    public BlockListDataTypeDesigner Limit(int? min, int? max) {
        _minBlocks = min;
        _maxBlocks = max;

        return this;
    }

    public BlockListDataTypeDesigner UseSingleBlockMode() {
        _useSingleBlockMode = true;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new BlockListConfiguration();

        configuration.Blocks = _blocks.Select(x => x.Build(ResolveElementType)).ToArray();
        configuration.UseSingleBlockMode = _useSingleBlockMode;

        if (_minBlocks.HasValue() || _maxBlocks.HasValue()) {
            configuration.ValidationLimit.Min = _minBlocks;
            configuration.ValidationLimit.Max = _maxBlocks;
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.BlockList;

    private IContentType ResolveElementType(string alias) {
        var elementType = _contentTypeService.Get(alias);

        if (elementType == null) {
            throw new Exception($"No element type found with alias {alias.Quote()}");
        }

        return elementType;
    }
}

public class BlockListBlockBuilder {
    private readonly string _elementTypeAlias;

    private string _settingsElementTypeAlias;

    public BlockListBlockBuilder(string elementTypeAlias) {
        _elementTypeAlias = elementTypeAlias;
    }

    public BlockListBlockBuilder WithSettings(string settingsElementTypeAlias) {
        _settingsElementTypeAlias = settingsElementTypeAlias;

        return this;
    }

    public BlockListConfiguration.BlockConfiguration Build(Func<string, IContentType> resolveElementType) {
        var block = new BlockListConfiguration.BlockConfiguration();

        block.ContentElementTypeKey = resolveElementType(_elementTypeAlias).Key;

        if (_settingsElementTypeAlias.HasValue()) {
            block.SettingsElementTypeKey = resolveElementType(_settingsElementTypeAlias).Key;
        }

        return block;
    }
}
