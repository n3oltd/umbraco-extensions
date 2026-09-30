using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Json;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Parameters;
using N3O.Umbraco.Scheduler.Models;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.Scheduler.Controllers;

[ApiDocument("JobProxy")]
public class JobProxyController : ApiController {
    private readonly IJsonProvider _jsonProvider;
    private readonly IMediator _mediator;
    private readonly IFluentParameters _fluentParameters;
    private readonly ILogger<JobProxyController> _logger;

    public JobProxyController(IJsonProvider jsonProvider,
                              IMediator mediator,
                              IFluentParameters fluentParameters,
                              ILogger<JobProxyController> logger) {
        _jsonProvider = jsonProvider;
        _mediator = mediator;
        _fluentParameters = fluentParameters;
        _logger = logger;
    }

    [HttpPost("executeProxied")]
    public async Task<ActionResult> ExecuteProxiedAsync(ProxyReq req) { 
        if (!IsAuthorized()) {
            return Unauthorized();
        }
        
        try {
            var model = _jsonProvider.DeserializeObject(req.RequestBody, req.RequestType);

            foreach (var (name, value) in req.ParameterData.OrEmpty()) {
                _fluentParameters.Add(name, value);
            }

            await _mediator.SendAsync(req.CommandType, typeof(None), model);

            return Ok();
        } catch (Exception ex) {
            _logger.LogError(ex, "Proxied execution of {CommandType} failed", req.CommandType);

            var errorRes = new ProxyErrorRes();
            errorRes.Error = ex.Message;

            return BadRequest(errorRes);
        }
    }

    private bool IsAuthorized() {
        Request.Headers.TryGetValue("X-Api-Key", out var apiKey);

        if (apiKey.HasValue() && apiKey.ToString() == TriggerKey.ApiSecurityKey) {
            return true;
        }

        return false;
    }
}