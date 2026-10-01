using N3O.Umbraco.EditorJs.Models;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace N3O.Umbraco.EditorJs;

public abstract class BlockDataConverter<TData> : IBlockDataConverter where TData : class {
    private const string DocumentEntityType = global::Umbraco.Cms.Core.Constants.UdiEntityType.Document;
    private const string MediaEntityType = global::Umbraco.Cms.Core.Constants.UdiEntityType.Media;
    private const string HrefAttributeStart = "href=\"";
    
    private readonly IPublishedContentCache _contentCache;
    private readonly IPublishedMediaCache _mediaCache;
    private readonly IPublishedUrlProvider _publishedUrlProvider;

    protected BlockDataConverter(IPublishedContentCache contentCache,
                                 IPublishedMediaCache mediaCache,
                                 IPublishedUrlProvider publishedUrlProvider) {
        _contentCache = contentCache;
        _mediaCache = mediaCache;
        _publishedUrlProvider = publishedUrlProvider;
    }
    
    public bool CanConvert(string typeId) {
        return TypeId.EqualsInvariant(typeId);
    }
    
    public EditorJsBlock Convert(string id, string typeId, JsonSerializer serializer, JObject data, JObject tunesData) {
        var typedData = data.ToObject<TData>(serializer);

        Process(typedData);

        return new EditorJsBlock<TData>(id, typeId, typedData, tunesData);
    }
    
    protected abstract string TypeId { get; }

    protected virtual void Process(TData data) { }

    protected string ConvertUmbracoLinks(string text) {
        if (text == null) {
            return null;
        }

        // Older content wraps the UDI in {localLink:...}, URL-encoded when it follows an absolute URL.
        return Regex.Replace(text,
                             @"(<a\s+(?:[^>]*?\s+)?href="")(?:[^""]*?(?:\{|%7B)localLink:)?" +
                             @"(umb://[^""]*?)(?:\}|%7D)?""",
                             ConvertUdiUrl);
    }

    protected string DecodePlatformsElements(string text) {
        if (text == null) {
            return null;
        }

        var encodedStart = HttpUtility.HtmlEncode(EditorJsConstants.Delimiters.PlatformsElements.Start);
        var encodedEnd = HttpUtility.HtmlEncode(EditorJsConstants.Delimiters.PlatformsElements.End);

        return Regex.Replace(text,
                             encodedStart + "(.*?)" + encodedEnd,
                             m => HttpUtility.HtmlDecode(m.Groups[1].Value));
    }

    private string ConvertUdiUrl(Match match) {
        var udiText = match.Groups[2].Value;

        // Stored content can carry any href, so an unparseable or unsupported reference loses its
        // destination rather than taking the page down.
        if (UdiParser.TryParse(udiText, out var udi) && udi is GuidUdi guidUdi) {
            IPublishedContent content = null;

            if (udi.EntityType == DocumentEntityType) {
                content = _contentCache.GetById(guidUdi.Guid);
            } else if (udi.EntityType == MediaEntityType) {
                content = _mediaCache.GetById(guidUdi.Guid);
            }

            if (content != null) {
                return $"{match.Groups[1].Value}{content.Url(_publishedUrlProvider)}\"";
            }
        }

        // The match spans the opening tag up to and including the href's closing quote, so returning the
        // tag without that attribute keeps the anchor and its text and drops only the dead destination.
        return match.Groups[1].Value[..^HrefAttributeStart.Length];
    }
}