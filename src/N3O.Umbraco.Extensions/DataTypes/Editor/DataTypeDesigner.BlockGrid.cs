using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class BlockGridDataTypeDesigner : DataTypeDesigner {
    private readonly IContentTypeService _contentTypeService;
    private readonly List<BlockGridBlockBuilder> _blocks = [];

    private int? _gridColumns;
    private int? _maxBlocks;
    private int? _minBlocks;

    public BlockGridDataTypeDesigner(IDataTypeService dataTypeService,
                                     IDataTypeContainerService dataTypeContainerService,
                                     IContentTypeService contentTypeService,
                                     PropertyEditorCollection propertyEditors,
                                     IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) {
        _contentTypeService = contentTypeService;
    }

    public BlockGridBlockBuilder AddBlock(string elementTypeAlias) {
        var block = new BlockGridBlockBuilder(elementTypeAlias);

        _blocks.Add(block);

        return block;
    }

    public BlockGridDataTypeDesigner Limit(int? min, int? max) {
        _minBlocks = min;
        _maxBlocks = max;

        return this;
    }

    public BlockGridDataTypeDesigner SetGridColumns(int gridColumns) {
        _gridColumns = gridColumns;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var existingConfiguration = existing?.ConfigurationObject as BlockGridConfiguration;
        var configuration = new BlockGridConfiguration();

        configuration.Blocks = _blocks.Select(x => BuildBlock(x, existingConfiguration)).ToArray();

        if (_minBlocks.HasValue() || _maxBlocks.HasValue()) {
            configuration.ValidationLimit.Min = _minBlocks;
            configuration.ValidationLimit.Max = _maxBlocks;
        }

        if (_gridColumns.HasValue()) {
            configuration.GridColumns = _gridColumns;
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.BlockGrid;

    private BlockGridConfiguration.BlockGridBlockConfiguration BuildBlock(BlockGridBlockBuilder block,
                                                                          BlockGridConfiguration existing) {
        return block.Build(ResolveElementType,
                           areaAlias => GetAreaKey(block.ElementTypeAlias, areaAlias, existing));
    }

    private Guid GetAreaKey(string elementTypeAlias, string areaAlias, BlockGridConfiguration existing) {
        var existingBlock = existing?.Blocks.FirstOrDefault(x => {
            var elementType = _contentTypeService.Get(x.ContentElementTypeKey);

            return elementType != null && elementType.Alias.EqualsInvariant(elementTypeAlias);
        });
        var existingArea = existingBlock?.Areas.FirstOrDefault(x => x.Alias.EqualsInvariant(areaAlias));

        return existingArea?.Key ?? UmbracoId.Deterministic(IdScope.BlockArea, elementTypeAlias, areaAlias);
    }

    private IContentType ResolveElementType(string alias) {
        var elementType = _contentTypeService.Get(alias);

        if (elementType == null) {
            throw new Exception($"No element type found with alias {alias.Quote()}");
        }

        return elementType;
    }
}

public class BlockGridBlockBuilder {
    private readonly List<BlockGridAreaBuilder> _areas = [];

    private bool _allowAtRoot = true;
    private bool _allowInAreas = true;
    private int? _areaGridColumns;
    private string _settingsElementTypeAlias;

    public BlockGridBlockBuilder(string elementTypeAlias) {
        ElementTypeAlias = elementTypeAlias;
    }

    public BlockGridAreaBuilder AddArea(string alias) {
        var area = new BlockGridAreaBuilder(alias);

        _areas.Add(area);

        return area;
    }

    public BlockGridBlockBuilder AreaGridColumns(int areaGridColumns) {
        _areaGridColumns = areaGridColumns;

        return this;
    }

    public BlockGridBlockBuilder DisallowAtRoot() {
        _allowAtRoot = false;

        return this;
    }

    public BlockGridBlockBuilder DisallowInAreas() {
        _allowInAreas = false;

        return this;
    }

    public BlockGridBlockBuilder WithSettings(string settingsElementTypeAlias) {
        _settingsElementTypeAlias = settingsElementTypeAlias;

        return this;
    }

    public string ElementTypeAlias { get; }

    public BlockGridConfiguration.BlockGridBlockConfiguration Build(Func<string, IContentType> resolveElementType,
                                                                    Func<string, Guid> getAreaKey) {
        var block = new BlockGridConfiguration.BlockGridBlockConfiguration();

        block.ContentElementTypeKey = resolveElementType(ElementTypeAlias).Key;
        block.AllowAtRoot = _allowAtRoot;
        block.AllowInAreas = _allowInAreas;
        block.Areas = _areas.Select(x => x.Build(getAreaKey)).ToArray();

        if (_areaGridColumns.HasValue()) {
            block.AreaGridColumns = _areaGridColumns;
        }

        if (_settingsElementTypeAlias.HasValue()) {
            block.SettingsElementTypeKey = resolveElementType(_settingsElementTypeAlias).Key;
        }

        return block;
    }
}

public class BlockGridAreaBuilder {
    private readonly string _alias;

    private int? _columnSpan;
    private int? _maxAllowed;
    private int? _minAllowed;
    private int? _rowSpan;

    public BlockGridAreaBuilder(string alias) {
        _alias = alias;
    }

    public BlockGridAreaBuilder ColumnSpan(int columnSpan) {
        _columnSpan = columnSpan;

        return this;
    }

    public BlockGridAreaBuilder Limit(int? min, int? max) {
        _minAllowed = min;
        _maxAllowed = max;

        return this;
    }

    public BlockGridAreaBuilder RowSpan(int rowSpan) {
        _rowSpan = rowSpan;

        return this;
    }

    public BlockGridConfiguration.BlockGridAreaConfiguration Build(Func<string, Guid> getAreaKey) {
        var area = new BlockGridConfiguration.BlockGridAreaConfiguration();

        area.Key = getAreaKey(_alias);
        area.Alias = _alias;
        area.ColumnSpan = _columnSpan;
        area.RowSpan = _rowSpan;
        area.MaxAllowed = _maxAllowed;
        area.MinAllowed = _minAllowed;

        return area;
    }
}
