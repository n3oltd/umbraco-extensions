using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using SecurityConstants = Umbraco.Cms.Core.Constants.Security;

namespace N3O.Umbraco.Security;

public class BackofficeUser : IBackofficeUser {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackofficeUser(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    // The back office cookie is only renewed by a full back office page load, so it expires while an editor is still
    // working. Umbraco re-issues the exposed cookie on every token refresh and removes it on sign-out.
    public async Task<bool> IsLoggedInAsync() {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null) {
            return false;
        }

        var authenticateResult = await httpContext.AuthenticateAsync(SecurityConstants.BackOfficeExposedAuthenticationType);

        return authenticateResult.Succeeded;
    }
}
