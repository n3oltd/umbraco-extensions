using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Content;

public class BlockListPropertyBuilder : PropertyBuilder {
    private readonly List<(string, (IContentBuilder ContentBuilder, Guid Key))> _contentBuilders = [];
    private readonly IServiceProvider _serviceProvider;
    private readonly IContentTypeService _contentTypeService;
    private readonly IJsonSerializer _jsonSerializer;

    public BlockListPropertyBuilder(IContentTypeService contentTypeService,
                                    IServiceProvider serviceProvider,
                                    IJsonSerializer jsonSerializer)
        : base(contentTypeService) {
        _serviceProvider = serviceProvider;
        _contentTypeService = contentTypeService;
        _jsonSerializer = jsonSerializer;
    }

    public IContentBuilder Add(string contentTypeAlias, Guid? customKey = null, int? order = null) {
        var contentBuilder = new ContentBuilder(_serviceProvider, contentTypeAlias);
        var key = customKey ?? Guid.NewGuid();

        if (order.HasValue()) {
            _contentBuilders.Insert(order.GetValueOrThrow() - 1, (contentTypeAlias, (contentBuilder, key)));
        } else {
            _contentBuilders.Add((contentTypeAlias, (contentBuilder, key)));
        }

        return contentBuilder;
    }

    public override (object, IPropertyType) Build(string propertyAlias, string parentContentTypeAlias) {
        var layouts = new List<BlockListLayoutItem>();
        var blockItemDatas = new List<BlockItemData>();

        foreach (var (contentTypeAlias, (contentBuilder, key)) in _contentBuilders) {
            layouts.Add(new BlockListLayoutItem(key));

            var contentType = _contentTypeService.Get(contentTypeAlias);
            var blockItemData = new BlockItemData(key, contentType.Key, contentType.Alias);

            foreach (var (alias, value) in contentBuilder.Build()) {
                var blockPropertyValue = new BlockPropertyValue();
                blockPropertyValue.Alias = alias;
                blockPropertyValue.Value = value;
                blockPropertyValue.PropertyType = GetPropertyType(alias, contentTypeAlias);
                
                blockItemData.Values.Add(blockPropertyValue);
            }

            blockItemDatas.Add(blockItemData);
        }

        var blockValue = new BlockListValue();
        blockValue.Layout = new Dictionary<string, IEnumerable<IBlockLayoutItem>>();
        blockValue.Layout["Umbraco.BlockList"] = layouts;
        
        blockValue.ContentData = blockItemDatas;
        blockValue.SettingsData = [];

        return (_jsonSerializer.Serialize(blockValue), GetPropertyType(propertyAlias, parentContentTypeAlias));
    }
}
