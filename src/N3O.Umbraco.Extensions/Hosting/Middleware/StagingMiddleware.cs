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

    public StagingMiddleware(IUmbracoContextFactory umbracoContextFactory,
                             Lazy<IRemoteIpAddressAccessor> remoteIpAddressAccessor,
                             Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> cookieAuthenticationOptions,
                             IApplicationReadiness applicationReadiness) {
        _umbracoContextFactory = umbracoContextFactory;
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
        _cookieAuthenticationOptions = cookieAuthenticationOptions;
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
            var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext();
            var umbracoContext = umbracoContextReference.UmbracoContext;
            var contentType = umbracoContext.Content.GetContentType(StagingSettingsAlias);
            var stagingSettings = contentType.IfNotNull(x => umbracoContext.Content.GetByContentType(x))
                                            ?.SingleOrDefault()
                                            ?.As<StagingSettingsContent>();

            if (stagingSettings != null) {
                var remoteIp = Normalise(_remoteIpAddressAccessor.Value.GetRemoteIpAddress());
                var lockOutKey = GetLockOutKey(remoteIp);

                if (IsBlocked(lockOutKey)) {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    
                    return;
                }

                if (IsAuthorized(context, stagingSettings, remoteIp)) {
                    FailedLogins.Remove(lockOutKey);
                } else {
                    LogFailure(lockOutKey);
                    
                    context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Login to Staging Site\", charset=\"UTF-8\"");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    
                    return;
                }
            }
        }
        
        await next(context);
    }

    private IPAddress Normalise(IPAddress ipAddress) {
        if (ipAddress.IsIPv4MappedToIPv6) {
            return ipAddress.MapToIPv4();
        } else {
            return ipAddress;
        }
    }

    // An IPv6 host can rotate through its whole /64, so failures are counted per /64 rather than per address.
    private string GetLockOutKey(IPAddress remoteIp) {
        if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6) {
            var bytes = remoteIp.GetAddressBytes();

            Array.Clear(bytes, 8, 8);

            var prefix = new IPAddress(bytes);

            return new IPNetwork(prefix, 64).ToString();
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
        var failedCount = FailedLogins.GetOrCreate(lockOutKey, c => {
            c.SlidingExpiration = LockOutPeriod;

            return 0;
        });

        FailedLogins.Set(lockOutKey, failedCount + 1);
    }

    private bool IsAllowed(IPAddress remoteIp, string ruleIpAddress) {
        if (IPAddress.TryParse(ruleIpAddress, out var ipAddress)) {
            return Normalise(ipAddress).Equals(remoteIp);
        } else if (IPNetwork.TryParse(ruleIpAddress, out var network)) {
            return network.Contains(remoteIp);
        } else {
            return false;
        }
    }

    private bool IsAuthorized(HttpContext context, StagingSettingsContent stagingSettings, IPAddress remoteIp) {
        var isAuthorized = false;

        if (stagingSettings.Rules.OrEmpty().Any(x => IsAllowed(remoteIp, x.RuleIpAddress))) {
            isAuthorized = true;
        } else if (IsSignedIntoBackOffice(context)) {
            isAuthorized = true;
        } else {
            string header = context.Request.Headers["Authorization"];

            if (TryReadBasicCredentials(header, out var username, out var password) &&
                username.EqualsInvariant(stagingSettings.Username) &&
                password == stagingSettings.Password) {
                isAuthorized = true;
            }
        }

        return isAuthorized;
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

        var usernameAndPassword = Encoding.UTF8.GetString(decoded, 0, decodedLength).Split(':');

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
