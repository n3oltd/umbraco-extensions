using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace N3O.Umbraco.Authentication.Controllers;

[ApiDocument(AuthenticationConstants.ApiName)]
[UmbracoMemberAuthorize]
public class AuthenticationController : SurfaceController {
    private readonly ISignInManager _signInManager;

    public AuthenticationController(IUmbracoContextAccessor umbracoContextAccessor,
                                    IUmbracoDatabaseFactory databaseFactory,
                                    ServiceContext services,
                                    AppCaches appCaches,
                                    IProfilingLogger profilingLogger,
                                    IPublishedUrlProvider publishedUrlProvider,
                                    ISignInManager signInManager)
        : base(umbracoContextAccessor,
               databaseFactory,
               services,
               appCaches,
               profilingLogger,
               publishedUrlProvider) {
        _signInManager = signInManager;
    }
    
    [HttpGet("signout")]
    public async Task<IActionResult> HandleLogout([FromQuery] string returnUrl) {
        await _signInManager.SignOutAsync(HttpContext);

        var redirectUrl = returnUrl ?? HttpContext.Request.Headers.Referer.ToString();

        if (!Url.IsLocalUrl(redirectUrl)) {
            redirectUrl = "/";
        }

        return Redirect(redirectUrl);
    }
    
    [HttpGet("password/reset")]
    public async Task<IActionResult> GetPasswordResetUrl() {
        var passwordChangeUrl = await _signInManager.GetPasswordResetUrlAsync();

        return Redirect(passwordChangeUrl);
    }
}
