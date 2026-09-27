using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Marketing.TagHelpers;

// Placed after umbracoEngage.analytics.js. A page Engage renders on the server carries its own init call, and the
// stock script buffers events and scans forms at init, so nothing needs queueing before the session answers.
[HtmlTargetElement($"{Prefixes.TagHelpers}engage-session")]
public class EngageSessionTagHelper : TagHelper {
    private const string Script = """
                                  (function () {
                                      function start() {
                                          fetch('/umbraco/api/engage/v1/session', {
                                              method: 'POST',
                                              credentials: 'same-origin',
                                              keepalive: true,
                                              headers: { 'Content-Type': 'application/json' },
                                              body: JSON.stringify({ url: location.href, referrer: document.referrer || null })
                                          }).then(function (response) {
                                              return response.ok ? response.json() : null;
                                          }).then(function (res) {
                                              if (res && res.pageviewId && window.umbracoEngage && umbracoEngage.analytics) {
                                                  umbracoEngage.analytics.init(res.pageviewId);
                                              }
                                          }).catch(function () { });
                                      }

                                      if (document.prerendering) {
                                          document.addEventListener('prerenderingchange', start, { once: true });
                                      } else {
                                          start();
                                      }
                                  })();
                                  """;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        if (!BrowserEngage.IsEnabled() || BrowserEngage.IsServerRendered(ViewContext.HttpContext)) {
            output.SuppressOutput();
        } else {
            output.TagName = null;

            var scriptTag = new TagBuilder("script");
            scriptTag.InnerHtml.AppendHtml(Script);

            output.Content.SetHtmlContent(scriptTag.ToHtmlString());
        }
    }
}
