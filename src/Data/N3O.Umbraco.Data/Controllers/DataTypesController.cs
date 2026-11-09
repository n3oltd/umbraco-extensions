using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Data.Criteria;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Data.Queries;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Data.Controllers;

[ApiDocument(DataConstants.ApiNames.DataTypes)]
public class DataTypesController : BackofficeAuthorizedApiController {
    private readonly IMediator _mediator;

    public DataTypesController(IMediator mediator) {
        _mediator = mediator;
    }

    [HttpPost("find")]
    public async Task<ActionResult<IEnumerable<DataTypeRes>>> FindDataTypes(DataTypeCriteria req) {
        var res = await _mediator.SendAsync<FindDataTypesQuery, DataTypeCriteria, IEnumerable<DataTypeRes>>(req);

        return Ok(res);
    }
}
