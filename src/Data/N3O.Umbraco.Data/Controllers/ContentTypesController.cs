using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Data.Criteria;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Data.Queries;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Data.Controllers;

// TODO Add authentication to this controller
[ApiDocument(DataConstants.ApiNames.ContentTypes)]
public class ContentTypesController : ApiController {
    private readonly IMediator _mediator;

    public ContentTypesController(IMediator mediator) {
        _mediator = mediator;
    }

    [HttpPost("find")]
    public async Task<ActionResult<IEnumerable<ContentTypeRes>>> FindContentTypes(ContentTypeCriteria req) {
        var res = await _mediator.SendAsync<FindContentTypesQuery, ContentTypeCriteria, IEnumerable<ContentTypeRes>>(req);

        return Ok(res);
    }
    
    [HttpPost("{contentType}")]
    public async Task<ActionResult<ContentTypeRes>> GetContentTypeByAlias() {
        try {
            var res = await _mediator.SendAsync<GetContentTypeByAliasQuery, None, ContentTypeRes>(None.Empty);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }

    [HttpGet("{contentId:guid}/relations")]
    public async Task<ActionResult<IEnumerable<ContentTypeRes>>> GetRelationContentTypes([FromQuery] string type) {
        try {
            var res = await _mediator.SendAsync<GetRelationContentTypesQuery, string, IEnumerable<ContentTypeRes>>(type);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }
}
