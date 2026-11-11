using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Search.Typesense.Commands;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Authorization;

namespace N3O.Umbraco.Search.Typesense.Controllers;

[ApiDocument(TypesenseConstants.BackOfficeApiName)]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class TypesenseDevToolsController : BackofficeAuthorizedApiController {
    private readonly IMediator _mediator;
    
    public TypesenseDevToolsController(IMediator mediator) {
        _mediator = mediator;
    }

    [HttpPost("content/reindex/{contentType}")]
    public async Task<ActionResult> ReindexContent() {
        var res = await _mediator.SendAsync<IndexContentsOfTypeCommand, None, None>(None.Empty);
        
        return Ok(res);
    }
}