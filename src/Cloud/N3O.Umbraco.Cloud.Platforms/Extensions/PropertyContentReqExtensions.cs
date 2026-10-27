using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class PropertyContentReqExtensions {
    public static bool EditorHasValue(this PropertyContentReq propertyContent) {
        if (propertyContent == null) {
            return false;
        }
        
        if (propertyContent.Editor == PropertyEditor.Boolean) {
            return propertyContent.Boolean.HasValue(x => x.Value);
        } else if (propertyContent.Editor == PropertyEditor.DateTime) {
            return propertyContent.DateTime.HasValue(x => x.Value);
        } else if (propertyContent.Editor == PropertyEditor.EditorJs) {
            return propertyContent.EditorJs.HasAny(x => x.Blocks);
        } else if (propertyContent.Editor == PropertyEditor.ImageSimple) {
            return propertyContent.ImageSimple.HasValue(x => x.SourceFile);
        } else if (propertyContent.Editor == PropertyEditor.NestedContentMultiple) {
            return propertyContent.NestedContentMultiple.HasAny(x => x.Elements);
        } else if (propertyContent.Editor == PropertyEditor.NestedContentSingle) {
            return propertyContent.NestedContentSingle.HasValue(x => x.Element);
        } else if (propertyContent.Editor == PropertyEditor.RichText) {
            return propertyContent.RichText.HasValue(x => x.Html);
        } else if (propertyContent.Editor == PropertyEditor.Svg) {
            return propertyContent.Svg.HasValue(x => x.SourceFile);
        } else if (propertyContent.Editor == PropertyEditor.Textarea) {
            return propertyContent.Textarea.HasValue(x => x.Text);
        } else if (propertyContent.Editor == PropertyEditor.Textbox) {
            return propertyContent.Textbox.HasValue(x => x.Text);
        } else if (propertyContent.Editor == PropertyEditor.VideoSimple) {
            return propertyContent.VideoSimple.HasValue(x => x.SourceFile);
        } else {
            throw UnrecognisedValueException.For(propertyContent.Editor);
        }
        
        return false;
    }

    public static IReadOnlyList<PropertyContentReq> ToPublishableProperties(this IEnumerable<PropertyContentReq> src,
                                                                            IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        return src.OrEmpty()
                  .Where(x => x.EditorHasValue())
                  .Select(x => WithAbsoluteUrls(x, mediaUrlBuilder))
                  .ToList();
    }

    private static PropertyContentReq WithAbsoluteUrls(PropertyContentReq propertyContent,
                                                       IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        if (propertyContent.Editor == PropertyEditor.RichText) {
            propertyContent.RichText = propertyContent.RichText.Html.ToRichTextContentReq(mediaUrlBuilder);
        }

        return propertyContent;
    }
}