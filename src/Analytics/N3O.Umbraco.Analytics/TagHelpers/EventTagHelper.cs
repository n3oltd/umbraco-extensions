using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Json;
using System.Threading.Tasks;
using Umbraco.Extensions;

namespace N3O.Umbraco.Analytics.TagHelpers;

public abstract class EventTagHelper : TagHelper {
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
            scriptTag.InnerHtml.AppendHtmlLine("  function send() {");
            scriptTag.InnerHtml.AppendHtmlLine("    window.dataLayer = window.dataLayer || [];");
            scriptTag.InnerHtml.AppendHtmlLine("    (function() {");
            scriptTag.InnerHtml.AppendHtmlLine("      window.dataLayer.push(arguments);");
            scriptTag.InnerHtml.AppendHtmlLine($"    }})('event', '{EventName}', {parametersJson});");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("  if (document.readyState === 'loading') {");
            scriptTag.InnerHtml.AppendHtmlLine("    document.addEventListener('DOMContentLoaded', send);");
            scriptTag.InnerHtml.AppendHtmlLine("  } else {");
            scriptTag.InnerHtml.AppendHtmlLine("    send();");
            scriptTag.InnerHtml.AppendHtmlLine("  }");
            scriptTag.InnerHtml.AppendHtmlLine("})();");

            output.Content.SetHtmlContent(scriptTag.ToHtmlString());
        }
    }

    protected virtual bool IsSuppressed() => false;
    protected abstract Task<object> GetParametersAsync();
    
    protected abstract string EventName { get; }
}
