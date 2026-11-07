using Microsoft.AspNetCore.Html;
using System;

namespace N3O.Umbraco.Extensions;

public static class ExceptionExtensions {
    public static IHtmlContent ToHtml(this Exception exception) {
        var html = new HtmlContentBuilder();

        html.AppendHtml("<pre>");
        html.Append(exception.ToString());
        html.AppendHtml("</pre>");

        return html;
    }
}
