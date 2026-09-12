using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Extensions;

namespace N3O.Umbraco.Hosting;

public class StagingMiddleware : IMiddleware {
    private static readonly string StagingSettingsAlias = AliasHelper<StagingSettingsContent>.ContentTypeAlias();
    private static readonly int MaxFailedAttempts = 15;
    private static readonly TimeSpan LockOutPeriod = TimeSpan.FromMinutes(5);
    private static readonly MemoryCache FailedLogins = new(new MemoryCacheOptions());

    private readonly Lazy<IRemoteIpAddressAccessor> _remoteIpAddressAccessor;
    private readonly Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> _cookieAuthenticationOptions;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IApplicationReadiness _applicationReadiness;

    public StagingMiddleware(Lazy<IRemoteIpAddressAccessor> remoteIpAddressAccessor,
                             Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> cookieAuthenticationOptions,
                             Lazy<IContentLocator> contentLocator,
                             IApplicationReadiness applicationReadiness) {
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
        _cookieAuthenticationOptions = cookieAuthenticationOptions;
        _contentLocator = contentLocator;
        _applicationReadiness = applicationReadiness;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        if (!_applicationReadiness.IsReady) {
            await next(context);

            return;
        }

        if (!context.Request.GetDisplayUrl().Contains("/umbraco", StringComparison.InvariantCultureIgnoreCase) &&
            !context.Request.GetDisplayUrl().Contains("/App_Plugins", StringComparison.InvariantCultureIgnoreCase) &&
            !context.Request.GetDisplayUrl().Contains("/sb", StringComparison.InvariantCultureIgnoreCase)) {
            var stagingSettings = _contentLocator.Value.Single<StagingSettingsContent>();

            if (stagingSettings != null) {
                var remoteIp = _remoteIpAddressAccessor.Value.GetRemoteIpAddress().ToString();

                if (IsAllowedWithoutCredentials(context, stagingSettings, remoteIp)) {
                    FailedLogins.Remove(remoteIp);
                } else if (IsBlocked(remoteIp)) {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    
                    return;
                } else if (HasValidCredentials(context, stagingSettings)) {
                    FailedLogins.Remove(remoteIp);
                } else {
                    LogFailure(remoteIp);
                    
                    context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Login to Staging Site\", charset=\"UTF-8\"");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    
                    return;
                }
            }
        }
        
        await next(context);
    }

    private bool IsBlocked(string remoteIp) {
        if (FailedLogins.Get<int>(remoteIp) > MaxFailedAttempts) {
            return true;
        } else {
            return false;
        }
    }

    private void LogFailure(string remoteIp) {
        // Set replaces the entry with one that carries no expiration, so applying the lock out period only
        // when the entry is created leaves every failure after the first stored permanently, which blocks
        // the address until the process restarts rather than for the lock out period.
        var failedCount = FailedLogins.Get<int>(remoteIp);

        FailedLogins.Set(remoteIp,
                         failedCount + 1,
                         new MemoryCacheEntryOptions { SlidingExpiration = LockOutPeriod });
    }

    // An allow listed address and a back office cookie are not guessable secrets, so gating them behind the
    // failed password lock out gives no protection and only strands the people able to lift the block.
    private bool IsAllowedWithoutCredentials(HttpContext context,
                                             StagingSettingsContent stagingSettings,
                                             string remoteIp) {
        if (stagingSettings.Rules.OrEmpty().Any(x => remoteIp.EqualsInvariant(x.RuleIpAddress))) {
            return true;
        }

        return IsSignedIntoBackOffice(context);
    }

    private bool HasValidCredentials(HttpContext context, StagingSettingsContent stagingSettings) {
        string header = context.Request.Headers["Authorization"];

        if (!header.HasValue()) {
            return false;
        }

        var auth = header.Split(' ')[1];
        var usernameAndPassword = Encoding.UTF8.GetString(Convert.FromBase64String(auth)).Split(':');
        var username = usernameAndPassword[0];
        var password = usernameAndPassword[1];

        return username.EqualsInvariant(stagingSettings.Username) && password == stagingSettings.Password;
    }

    private bool IsSignedIntoBackOffice(HttpContext context) {
        var authType = global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType;
        var cookieOptions = _cookieAuthenticationOptions.Value.Get(authType);

        var backOfficeCookie = context.Request.Cookies[cookieOptions.Cookie.Name];

        if (backOfficeCookie != null) {
            var unprotected = cookieOptions.TicketDataFormat.Unprotect(backOfficeCookie);
            var backOfficeIdentity = unprotected?.Principal.GetUmbracoIdentity();

            if (backOfficeIdentity != null) {
                return true;
            }
        }
        
        return false;
    }
}
