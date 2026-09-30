using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Exceptions;

namespace N3O.Umbraco.Plugins.Controllers;

public partial class PluginController {
    protected NotFoundObjectResult NotFound(ResourceNotFoundException ex) {
        return NotFound(ex.Resource);
    }
}
