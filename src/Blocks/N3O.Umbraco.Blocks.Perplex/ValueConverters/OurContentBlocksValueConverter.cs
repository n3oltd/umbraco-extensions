using Perplex.ContentBlocks.PropertyEditor;
using Perplex.ContentBlocks.PropertyEditor.Value;
using System;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace N3O.Umbraco.Blocks.Perplex;

public class OurContentBlocksValueConverter : ContentBlocksValueConverter {
    public OurContentBlocksValueConverter(IServiceProvider serviceProvider,
                                          ContentBlocksValueDeserializer deserializer,
                                          BlockEditorConverter converter,
                                          IApiElementBuilder apiElementBuilder)
        : base(serviceProvider, deserializer, converter, apiElementBuilder) { }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.None;
    }
}
