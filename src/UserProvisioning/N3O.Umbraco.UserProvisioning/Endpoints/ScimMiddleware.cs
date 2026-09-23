using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Scim;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Stores;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace N3O.Umbraco.UserProvisioning.Endpoints;

public class ScimMiddleware : IMiddleware {
    private readonly IBearerTokenAuthorizer _authorizer;
    private readonly IScimStore<ScimGroup> _groupStore;
    private readonly ILogger<ScimMiddleware> _logger;
    private readonly IRuntimeState _runtimeState;
    private readonly UserProvisioningSettings _settings;
    private readonly IScimStore<ScimUser> _userStore;

    public ScimMiddleware(IBearerTokenAuthorizer authorizer,
                          IScimStore<ScimGroup> groupStore,
                          ILogger<ScimMiddleware> logger,
                          IRuntimeState runtimeState,
                          UserProvisioningSettings settings,
                          IScimStore<ScimUser> userStore) {
        _authorizer = authorizer;
        _groupStore = groupStore;
        _logger = logger;
        _runtimeState = runtimeState;
        _settings = settings;
        _userStore = userStore;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var route = _settings.BaseRoute.TrimEnd('/');

        if (!context.Request.Path.StartsWithSegments(route,
                                                     ScimText.Comparison,
                                                     out var rest)) {
            await next(context);

            return;
        }

        // BootFailed can be reached after the server is up, so this is asked on every request rather than
        // once while the pipeline is built
        if (_runtimeState.Level != RuntimeLevel.Run) {
            await WriteErrorAsync(context,
                                  new ScimException(HttpStatusCode.ServiceUnavailable,
                                                    $"The site is not running ({_runtimeState.Level})"));

            return;
        }

        if (!_authorizer.IsAuthorized(context)) {
            await WriteErrorAsync(context, new ScimException(HttpStatusCode.Unauthorized, "Unauthorised"));

            return;
        }

        try {
            await RouteAsync(context, rest.Value.Trim('/'));
        } catch (ScimException ex) {
            _logger.LogWarning("SCIM {Method} {Path} refused: {Detail}",
                               context.Request.Method,
                               context.Request.Path,
                               ex.Message);

            await WriteErrorAsync(context, ex);
        } catch (Exception ex) {
            _logger.LogError(ex, "SCIM {Method} {Path} failed", context.Request.Method, context.Request.Path);

            await WriteErrorAsync(context,
                                  new ScimException(HttpStatusCode.InternalServerError,
                                                    "The request could not be completed"));
        }
    }

    private async Task RouteAsync(HttpContext context, string path) {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resource = segments.FirstOrDefault() ?? "";
        var id = segments.Length > 1 ? segments[1] : null;

        if (resource.Is("ServiceProviderConfig")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.ServiceProviderConfig());
        } else if (resource.Is("ResourceTypes")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.ResourceTypes(_settings.BaseRoute));
        } else if (resource.Is("Schemas")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.Schemas());
        } else if (resource.Is("Users")) {
            await DispatchAsync(context, _userStore, id);
        } else if (resource.Is("Groups")) {
            await DispatchAsync(context, _groupStore, id);
        } else {
            throw ScimException.NotFound($"{context.Request.Path} is not a resource this endpoint serves");
        }
    }

    private async Task DispatchAsync<T>(HttpContext context, IScimStore<T> store, string id) where T : ScimResource {
        var method = context.Request.Method;

        if (method.Is("GET") && !id.HasValue()) {
            await WriteAsync(context, HttpStatusCode.OK, await store.ListAsync(ReadQuery(context)));
        } else if (method.Is("GET")) {
            await WriteAsync(context, HttpStatusCode.OK, await store.GetAsync(id));
        } else if (method.Is("POST") && !id.HasValue()) {
            await WriteAsync(context, HttpStatusCode.Created, await store.CreateAsync(await ReadBodyAsync<T>(context)));
        } else if (method.Is("PUT") && id.HasValue()) {
            var resource = await ReadBodyAsync<T>(context);
            resource.Id = id;

            await WriteAsync(context, HttpStatusCode.OK, await store.ReplaceAsync(resource));
        } else if (method.Is("PATCH") && id.HasValue()) {
            var request = await ReadBodyAsync<ScimPatchRequest>(context);

            await WriteAsync(context, HttpStatusCode.OK, await store.PatchAsync(id, request?.Operations));
        } else if (method.Is("DELETE") && id.HasValue()) {
            await store.DeleteAsync(id);

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        } else {
            throw new ScimException(HttpStatusCode.MethodNotAllowed, $"{method} is not allowed here");
        }
    }

    private static ScimQuery ReadQuery(HttpContext context) {
        var query = new ScimQuery();
        query.Filter = ScimFilterParser.Parse(context.Request.Query["filter"]);

        if (int.TryParse(context.Request.Query["count"], out var count)) {
            query.Count = count;
        }

        if (int.TryParse(context.Request.Query["startIndex"], out var startIndex)) {
            query.StartIndex = startIndex;
        }

        return query;
    }

    private async Task<T> ReadBodyAsync<T>(HttpContext context) {
        using (var reader = new StreamReader(context.Request.Body)) {
            var body = await reader.ReadToEndAsync();

            if (!body.HasValue()) {
                throw ScimException.InvalidValue("The request carries no body");
            }

            if (_settings.LogRequests) {
                _logger.LogInformation("SCIM {Method} {Path} request {Body}",
                                       context.Request.Method,
                                       context.Request.Path,
                                       body);
            }

            try {
                return ScimJson.Read<T>(body);
            } catch (Exception) {
                throw ScimException.InvalidValue("The request body is not the resource this endpoint expected");
            }
        }
    }

    private async Task WriteAsync(HttpContext context, HttpStatusCode status, object body) {
        var json = ScimJson.Write(body);

        if (_settings.LogRequests) {
            _logger.LogInformation("SCIM {Method} {Path} answered {Status} {Body}",
                                   context.Request.Method,
                                   context.Request.Path,
                                   (int) status,
                                   json);
        }

        context.Response.ContentType = ScimConstants.ContentType;
        context.Response.StatusCode = (int) status;

        await context.Response.WriteAsync(json);
    }

    private async Task WriteErrorAsync(HttpContext context, ScimException exception) {
        var error = new ScimError();
        error.Detail = exception.Message;
        error.ScimType = exception.ScimType;
        error.Status = ((int) exception.Status).ToString();

        await WriteAsync(context, exception.Status, error);
    }
}
