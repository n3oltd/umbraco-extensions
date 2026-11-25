using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Context;
using System;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.Hosting;

[ApiDocument("DevTools")]
public class DevToolsController : ApiController {
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IRemoteIpAddressAccessor _remoteIpAddressAccessor;

    public DevToolsController(IConfiguration configuration,
                              IWebHostEnvironment webHostEnvironment,
                              IRemoteIpAddressAccessor remoteIpAddressAccessor) {
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
    }
    
    [HttpGet("core/configuration")]
    public ActionResult<string> GetConfiguration() {
        var root = (IConfigurationRoot) _configuration;

        return Result(() => root.GetDebugView());
    }
    
    [HttpGet("core/echo")]
    public ActionResult<object> Echo() {
        return Result<object>(() => new {
            PathBase = Request.PathBase.Value,
            Path = Request.Path.Value,
            QueryString = Request.QueryString.HasValue ? Request.QueryString.Value : null,
            Body = new StreamReader(Request.Body).ReadToEndAsync().GetAwaiter().GetResult(),
            Headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Verb = Request.Method,
            RemoteIp = _remoteIpAddressAccessor.GetRemoteIpAddress()
        });
    }

    private ActionResult<T> Result<T>(Func<T> getResult) {
        var isDevelopment = _webHostEnvironment.IsDevelopment();
            
        if (!isDevelopment) {
            return Unauthorized();
        }

        return Ok(getResult());
    }
}
