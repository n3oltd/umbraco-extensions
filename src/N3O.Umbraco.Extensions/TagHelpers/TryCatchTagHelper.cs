using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Security;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.TagHelpers;

[HtmlTargetElement("try-catch")]
public class TryCatchTagHelper : TagHelper {
    private readonly Lazy<IBackofficeUser> _backofficeUser;
    private readonly ILogger<TryCatchTagHelper> _logger;

    public TryCatchTagHelper(Lazy<IBackofficeUser> backofficeUser, ILogger<TryCatchTagHelper> logger) {
        _backofficeUser = backofficeUser;
        _logger = logger;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
        output.TagName = null;

        try {
            var childContent = await output.GetChildContentAsync();

            output.Content.SetHtmlContent(childContent);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to render the content of a try-catch tag");

            if (_backofficeUser.Value.IsLoggedIn()) {
                output.Content.SetHtmlContent(ex.ToHtml());
            } else {
                output.SuppressOutput();
            }
        }
    }
}