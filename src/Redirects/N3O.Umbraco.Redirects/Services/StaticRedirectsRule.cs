using Microsoft.AspNetCore.Rewrite;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Redirects;

namespace N3O.Umbraco.Hosting;

[Order(1)]
public class StaticRedirectsRule : IRule {
    public void ApplyRule(RewriteContext context) {
        var redirect = StaticRedirects.Find(context.HttpContext.Request.Path);

        if (redirect != null) {
            var url = redirect.GetUrl(context.HttpContext.Request.QueryString);
            
            context.HttpContext.Response.Redirect(url, !redirect.Temporary);
            
            context.Result = RuleResult.EndResponse;
        }
    }
}
