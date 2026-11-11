using Microsoft.AspNetCore.Html;
using System;

namespace N3O.Umbraco.Security;

public interface IRenderFailures {
    bool CanIsolate(Exception exception);
    IHtmlContent ToHtml(Exception exception);
}
