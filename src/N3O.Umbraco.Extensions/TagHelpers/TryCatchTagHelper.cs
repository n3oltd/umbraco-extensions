using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Security;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.TagHelpers;

[HtmlTargetElement("try-catch")]
public class TryCatchTagHelper : TagHelper {
    private readonly ILogger<TryCatchTagHelper> _logger;
    private readonly IRenderFailures _renderFailures;

    public TryCatchTagHelper(ILogger<TryCatchTagHelper> logger, IRenderFailures renderFailures) {
        _logger = logger;
        _renderFailures = renderFailures;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
        output.TagName = null;

        try {
            var childContent = await output.GetChildContentAsync();

            output.Content.SetHtmlContent(childContent);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to render the content of a try-catch tag");

            output.Content.SetHtmlContent(_renderFailures.ToHtml(ex));
        }
    }
}
