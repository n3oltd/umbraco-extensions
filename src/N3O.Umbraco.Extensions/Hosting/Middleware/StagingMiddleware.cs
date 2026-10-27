using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Security;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace N3O.Umbraco.Hosting;

public class StagingMiddleware : IMiddleware {
    private static readonly string StagingSettingsAlias = AliasHelper<StagingSettingsContent>.ContentTypeAlias();
    private static readonly int MaxFailedAttempts = 15;
    private static readonly TimeSpan LockOutPeriod = TimeSpan.FromMinutes(5);
    private static readonly MemoryCache FailedLogins = new(new MemoryCacheOptions());

    private readonly Lazy<IRemoteIpAddressAccessor> _remoteIpAddressAccessor;
    private readonly Lazy<IBackofficeUser> _backofficeUser;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IApplicationReadiness _applicationReadiness;

    public StagingMiddleware(Lazy<IRemoteIpAddressAccessor> remoteIpAddressAccessor,
                             Lazy<IBackofficeUser> backofficeUser,
                             Lazy<IContentLocator> contentLocator,
                             IApplicationReadiness applicationReadiness) {
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
        _backofficeUser = backofficeUser;
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
                var remoteIp = _remoteIpAddressAccessor.Value.GetRemoteIpAddress();

                var allowedWithoutCredentials = await IsAllowedWithoutCredentialsAsync(stagingSettings, remoteIp);

                if (!allowedWithoutCredentials) {
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

    private async Task<bool> IsAllowedWithoutCredentialsAsync(StagingSettingsContent stagingSettings,
                                                              IPAddress remoteIp) {
        if (stagingSettings.Rules.OrEmpty().Any(x => IsAllowed(remoteIp, x.RuleIpAddress?.Trim()))) {
            return true;
        } else {
            return await _backofficeUser.Value.IsLoggedInAsync();
        }
    }

    private bool HasValidCredentials(HttpContext context, StagingSettingsContent stagingSettings) {
        string header = context.Request.Headers["Authorization"];

        if (TryReadBasicCredentials(header, out var username, out var password) &&
            username.EqualsInvariant(stagingSettings.Username) &&
            password.EqualsSecret(stagingSettings.Password)) {
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
}
