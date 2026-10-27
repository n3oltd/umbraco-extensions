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

    // The back office cookie expires while the back office is still in use
    public async Task<bool> IsLoggedInAsync() {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null) {
            return false;
        }

        var authenticationType = SecurityConstants.BackOfficeExposedAuthenticationType;
        var authenticateResult = await httpContext.AuthenticateAsync(authenticationType);

        return authenticateResult.Succeeded;
    }
}
