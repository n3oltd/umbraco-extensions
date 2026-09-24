using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using NodaTime;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace N3O.Umbraco.Hosting;

public class StagingMiddleware : IMiddleware {
    private static readonly string StagingSettingsAlias = AliasHelper<StagingSettingsContent>.ContentTypeAlias();
    private static readonly int MaxFailedAttempts = 15;
    private static readonly TimeSpan LockOutPeriod = TimeSpan.FromMinutes(5);
    private static readonly MemoryCache FailedLogins = new(new MemoryCacheOptions());

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly Lazy<IRemoteIpAddressAccessor> _remoteIpAddressAccessor;
    private readonly Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> _cookieAuthenticationOptions;
    private readonly IApplicationReadiness _applicationReadiness;
    private readonly IClock _clock;

    public StagingMiddleware(IUmbracoContextFactory umbracoContextFactory,
                             Lazy<IRemoteIpAddressAccessor> remoteIpAddressAccessor,
                             Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> cookieAuthenticationOptions,
                             IApplicationReadiness applicationReadiness,
                             IClock clock) {
        _umbracoContextFactory = umbracoContextFactory;
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
        _cookieAuthenticationOptions = cookieAuthenticationOptions;
        _applicationReadiness = applicationReadiness;
        _clock = clock;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        if (!_applicationReadiness.IsReady) {
            await next(context);

            return;
        }

        if (!context.Request.GetDisplayUrl().Contains("/umbraco", StringComparison.InvariantCultureIgnoreCase) &&
            !context.Request.GetDisplayUrl().Contains("/App_Plugins", StringComparison.InvariantCultureIgnoreCase) &&
            !context.Request.GetDisplayUrl().Contains("/sb", StringComparison.InvariantCultureIgnoreCase)) {
            var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext();
            var umbracoContext = umbracoContextReference.UmbracoContext;
            var contentType = umbracoContext.Content.GetContentType(StagingSettingsAlias);
            var stagingSettings = contentType.IfNotNull(x => umbracoContext.Content.GetByContentType(x))
                                            ?.SingleOrDefault()
                                            ?.As<StagingSettingsContent>();

            if (stagingSettings != null) {
                var remoteIp = _remoteIpAddressAccessor.Value.GetRemoteIpAddress();

                if (!IsAllowedWithoutCredentials(context, stagingSettings, remoteIp)) {
                    var lockOutKey = GetLockOutKey(remoteIp);

                    if (IsBlocked(lockOutKey)) {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;

                        return;
                    } else if (HasValidCredentials(context, stagingSettings)) {
                        FailedLogins.Remove(lockOutKey);
                    } else {
                        LogFailure(lockOutKey);

                        context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Login to Staging Site\", charset=\"UTF-8\"");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        return;
                    }
                }
            }
        }
        
        await next(context);
    }

    // Keying per address lets a client reset the lock out by rotating through its /64.
    private string GetLockOutKey(IPAddress remoteIp) {
        if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6) {
            var bytes = remoteIp.GetAddressBytes();

            Array.Clear(bytes, 8, 8);

            return new IPAddress(bytes).ToString();
        } else {
            return remoteIp.ToString();
        }
    }

    private bool IsBlocked(string lockOutKey) {
        if (FailedLogins.Get<int>(lockOutKey) > MaxFailedAttempts) {
            return true;
        } else {
            return false;
        }
    }

    private void LogFailure(string lockOutKey) {
        var failedCount = FailedLogins.Get<int>(lockOutKey);
        var entryOptions = new MemoryCacheEntryOptions();
        entryOptions.AbsoluteExpirationRelativeToNow = LockOutPeriod;

        // Set replaces the whole entry, so an entry written without these options never expires.
        FailedLogins.Set(lockOutKey, failedCount + 1, entryOptions);
    }

    private bool IsAllowed(IPAddress remoteIp, string ruleIpAddress) {
        if (IPAddress.TryParse(ruleIpAddress, out var ipAddress)) {
            return ipAddress.UnmapIPv4().Equals(remoteIp);
        } else if (IPNetwork.TryParse(ruleIpAddress, out var network)) {
            return network.Contains(remoteIp);
        } else {
            return false;
        }
    }

    private bool IsAllowedWithoutCredentials(HttpContext context,
                                             StagingSettingsContent stagingSettings,
                                             IPAddress remoteIp) {
        if (stagingSettings.Rules.OrEmpty().Any(x => IsAllowed(remoteIp, x.RuleIpAddress?.Trim()))) {
            return true;
        } else {
            return IsSignedIntoBackOffice(context);
        }
    }

    private bool HasValidCredentials(HttpContext context, StagingSettingsContent stagingSettings) {
        string header = context.Request.Headers["Authorization"];

        if (TryReadBasicCredentials(header, out var username, out var password) &&
            username.EqualsInvariant(stagingSettings.Username) &&
            password == stagingSettings.Password) {
            return true;
        } else {
            return false;
        }
    }

    private bool TryReadBasicCredentials(string header, out string username, out string password) {
        username = null;
        password = null;

        if (!header.HasValue()) {
            return false;
        }

        var parts = header.Split(' ');

        if (parts.Length != 2 || !parts[0].EqualsInvariant("Basic")) {
            return false;
        }

        var decoded = new byte[parts[1].Length];

        if (!Convert.TryFromBase64String(parts[1], decoded, out var decodedLength)) {
            return false;
        }

        var usernameAndPassword = Encoding.UTF8.GetString(decoded, 0, decodedLength).Split(':', 2);

        if (usernameAndPassword.Length < 2) {
            return false;
        }

        username = usernameAndPassword[0];
        password = usernameAndPassword[1];

        return true;
    }

    private bool IsSignedIntoBackOffice(HttpContext context) {
        var authType = global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType;
        var cookieOptions = _cookieAuthenticationOptions.Value.Get(authType);

        var backOfficeCookie = context.Request.Cookies[cookieOptions.Cookie.Name];
        var ticket = backOfficeCookie.IfNotNull(x => cookieOptions.TicketDataFormat.Unprotect(x));

        if (ticket != null &&
            ticket.Properties.ExpiresUtc > _clock.GetCurrentInstant().ToDateTimeOffset() &&
            ticket.Principal.GetUmbracoIdentity() != null) {
            return true;
        } else {
            return false;
        }
    }
}
