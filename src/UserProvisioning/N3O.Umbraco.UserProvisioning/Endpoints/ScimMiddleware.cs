using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Scim;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Stores;
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
    private readonly UserProvisioningSettings _settings;
    private readonly IScimStore<ScimUser> _userStore;

    public ScimMiddleware(IBearerTokenAuthorizer authorizer,
                          IScimStore<ScimGroup> groupStore,
                          ILogger<ScimMiddleware> logger,
                          UserProvisioningSettings settings,
                          IScimStore<ScimUser> userStore) {
        _authorizer = authorizer;
        _groupStore = groupStore;
        _logger = logger;
        _settings = settings;
        _userStore = userStore;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var route = _settings.BaseRoute.TrimEnd('/');

        if (!context.Request.Path.StartsWithSegments(route,
                                                     StringComparison.InvariantCultureIgnoreCase,
                                                     out var rest)) {
            await next(context);

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

        if (resource.EqualsInvariant("ServiceProviderConfig")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.ServiceProviderConfig());
        } else if (resource.EqualsInvariant("ResourceTypes")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.ResourceTypes(_settings.BaseRoute));
        } else if (resource.EqualsInvariant("Schemas")) {
            await WriteAsync(context, HttpStatusCode.OK, ScimDiscovery.Schemas());
        } else if (resource.EqualsInvariant("Users")) {
            await DispatchAsync(context, _userStore, id);
        } else if (resource.EqualsInvariant("Groups")) {
            await DispatchAsync(context, _groupStore, id);
        } else {
            throw ScimException.NotFound($"{context.Request.Path} is not a resource this endpoint serves");
        }
    }

    private async Task DispatchAsync<T>(HttpContext context, IScimStore<T> store, string id) where T : ScimResource {
        var method = context.Request.Method;

        if (method.EqualsInvariant("GET") && !id.HasValue()) {
            await WriteAsync(context, HttpStatusCode.OK, await store.ListAsync(ReadQuery(context)));
        } else if (method.EqualsInvariant("GET")) {
            await WriteAsync(context, HttpStatusCode.OK, await store.GetAsync(id));
        } else if (method.EqualsInvariant("POST") && !id.HasValue()) {
            await WriteAsync(context, HttpStatusCode.Created, await store.CreateAsync(await ReadBodyAsync<T>(context)));
        } else if (method.EqualsInvariant("PUT") && id.HasValue()) {
            var resource = await ReadBodyAsync<T>(context);
            resource.Id = id;

            await WriteAsync(context, HttpStatusCode.OK, await store.ReplaceAsync(resource));
        } else if (method.EqualsInvariant("PATCH") && id.HasValue()) {
            var request = await ReadBodyAsync<ScimPatchRequest>(context);

            await WriteAsync(context, HttpStatusCode.OK, await store.PatchAsync(id, request?.Operations));
        } else if (method.EqualsInvariant("DELETE") && id.HasValue()) {
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

    private static async Task<T> ReadBodyAsync<T>(HttpContext context) {
        using (var reader = new StreamReader(context.Request.Body)) {
            var body = await reader.ReadToEndAsync();

            if (!body.HasValue()) {
                throw ScimException.InvalidValue("The request carries no body");
            }

            try {
                return ScimJson.Read<T>(body);
            } catch (Exception) {
                throw ScimException.InvalidValue("The request body is not the resource this endpoint expected");
            }
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode status, object body) {
        context.Response.ContentType = ScimConstants.ContentType;
        context.Response.StatusCode = (int) status;

        await context.Response.WriteAsync(ScimJson.Write(body));
    }

    private static async Task WriteErrorAsync(HttpContext context, ScimException exception) {
        var error = new ScimError();
        error.Detail = exception.Message;
        error.ScimType = exception.ScimType;
        error.Status = ((int) exception.Status).ToString();

        await WriteAsync(context, exception.Status, error);
    }
}
