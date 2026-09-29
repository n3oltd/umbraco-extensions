using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Exceptions;
using System.Net.Mime;
namespace N3O.Umbraco.Hosting;

[ApiController]
[OurJsonFilter]
[OurValidationFilter]
[Route("/umbraco/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ResponseCache(CacheProfileName = CacheProfiles.NoCache)]
public class ApiController : ControllerBase {
    protected NotFoundObjectResult NotFound(ResourceNotFoundException ex) {
        return NotFound(ex.Resource);
    }
}
