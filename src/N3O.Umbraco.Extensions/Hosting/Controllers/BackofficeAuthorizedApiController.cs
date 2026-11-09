using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Constants;
using System.Net.Mime;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;

namespace N3O.Umbraco.Hosting;

[ApiController]
[OurJsonFilter]
[OurValidationFilter]
[Route("/umbraco/backoffice/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ResponseCache(CacheProfileName = CacheProfiles.NoCache)]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class BackofficeAuthorizedApiController : UmbracoApiController { }
