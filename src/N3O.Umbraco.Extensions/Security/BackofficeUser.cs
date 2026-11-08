using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Extensions;
using NodaTime;
using Umbraco.Extensions;
using SecurityConstants = Umbraco.Cms.Core.Constants.Security;

namespace N3O.Umbraco.Security;

public class BackofficeUser : IBackofficeUser {
    private readonly IClock _clock;
    private readonly IOptionsSnapshot<CookieAuthenticationOptions> _cookieOptionsSnapshot;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackofficeUser(IClock clock,
                          IOptionsSnapshot<CookieAuthenticationOptions> cookieOptionsSnapshot,
                          IHttpContextAccessor httpContextAccessor) {
        _clock = clock;
        _cookieOptionsSnapshot = cookieOptionsSnapshot;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsLoggedIn() {
        if (_httpContextAccessor.HttpContext == null) {
            return false;
        }

        var cookieOptions = _cookieOptionsSnapshot.Get(SecurityConstants.BackOfficeAuthenticationType);
        var backOfficeCookie = _httpContextAccessor.HttpContext.Request.Cookies[cookieOptions.Cookie.Name];

        if (!backOfficeCookie.HasValue()) {
            return false;
        }

        var authenticationTicket = cookieOptions.TicketDataFormat.Unprotect(backOfficeCookie);
        var expiresUtc = authenticationTicket?.Properties.ExpiresUtc;

        if (expiresUtc == null || expiresUtc <= _clock.GetCurrentInstant().ToDateTimeOffset()) {
            return false;
        }

        var backOfficeIdentity = authenticationTicket.Principal.GetUmbracoIdentity();

        var result = backOfficeIdentity.HasValue(x => x.AuthenticationType) &&
                     backOfficeIdentity.AuthenticationType.EqualsInvariant(SecurityConstants.BackOfficeAuthenticationType);

        return result;
    }
}
