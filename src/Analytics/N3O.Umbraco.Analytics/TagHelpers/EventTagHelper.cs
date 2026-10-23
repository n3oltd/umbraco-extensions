using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Json;
using System.Threading.Tasks;
using Umbraco.Extensions;

namespace N3O.Umbraco.Analytics.TagHelpers;

public abstract class EventTagHelper : TagHelper {
    // The event waits for consent restored by deferred scripts, so it never fires before DOMContentLoaded
    private const int LoadTimeoutMilliseconds = 3000;
    
    private readonly IJsonProvider _jsonProvider;

    protected EventTagHelper(IJsonProvider jsonProvider) {
        _jsonProvider = jsonProvider;
    }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
        if (IsSuppressed()) {
            output.SuppressOutput();
        } else {
            output.TagName = null;

            var parameters = await GetParametersAsync();
            var parametersJson = _jsonProvider.SerializeObjectForScript(parameters);
            
            var scriptTag = new TagBuilder("script");

            scriptTag.InnerHtml.AppendHtmlLine("(function() {");
            scriptTag.InnerHtml.AppendHtmlLine("  var sent = false;");
            scriptTag.InnerHtml.AppendHtmlLine("  function send() {");
            scriptTag.InnerHtml.AppendHtmlLine("    if (sent || typeof gtag !== 'function') {");
            scriptTag.InnerHtml.AppendHtmlLine("      return;");
            scriptTag.InnerHtml.AppendHtmlLine("    }");
            scriptTag.InnerHtml.AppendHtmlLine("    sent = true;");
            scriptTag.InnerHtml.AppendHtmlLine($"    gtag('event', '{EventName}', {parametersJson});");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("  function sendLater() {");
            scriptTag.InnerHtml.AppendHtmlLine($"    setTimeout(send, {LoadTimeoutMilliseconds});");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("  if (document.readyState === 'complete') {");
            scriptTag.InnerHtml.AppendHtmlLine("    send();");
            scriptTag.InnerHtml.AppendHtmlLine("  } else {");
            scriptTag.InnerHtml.AppendHtmlLine("    window.addEventListener('load', send);");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("  if (document.readyState === 'loading') {");
            scriptTag.InnerHtml.AppendHtmlLine("    document.addEventListener('DOMContentLoaded', sendLater);");
            scriptTag.InnerHtml.AppendHtmlLine("  } else {");
            scriptTag.InnerHtml.AppendHtmlLine("    sendLater();");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("  document.addEventListener('visibilitychange', function() {");
            scriptTag.InnerHtml.AppendHtmlLine("    if (document.visibilityState === 'hidden') {");
            scriptTag.InnerHtml.AppendHtmlLine("      send();");
            scriptTag.InnerHtml.AppendHtmlLine("    }");
            scriptTag.InnerHtml.AppendHtmlLine("  });");
            scriptTag.InnerHtml.AppendHtmlLine("})();");

            output.Content.SetHtmlContent(scriptTag.ToHtmlString());
        }
    }

    protected virtual bool IsSuppressed() => false;
    protected abstract Task<object> GetParametersAsync();
    
    protected abstract string EventName { get; }
}
