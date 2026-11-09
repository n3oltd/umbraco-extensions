using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Criteria;
using N3O.Umbraco.Data.Queries;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Data.Controllers;

// TODO Add authentication to this controller
[ApiDocument(DataConstants.ApiNames.Content)]
public class ContentController : ApiController {
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator) {
        _mediator = mediator;
    }
    
    [HttpPost("{contentId:guid}/children/find")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ContentRes>>> FindChildren(ContentCriteria req) {
        try {
            var res = await _mediator.SendAsync<FindChildrenQuery, ContentCriteria, IEnumerable<ContentRes>>(req);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }
    
    [HttpPost("{contentId:guid}/descendants/find")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ContentRes>>> FindDescendants(ContentCriteria req) {
        try {
            var res = await _mediator.SendAsync<FindDescendantsQuery, ContentCriteria, IEnumerable<ContentRes>>(req);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }

    [HttpGet("{contentId:guid}")]
    public async Task<ActionResult<ContentRes>> GetById() {
        try {
            var res = await _mediator.SendAsync<GetContentByIdQuery, None, ContentRes>(None.Empty);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }
}
