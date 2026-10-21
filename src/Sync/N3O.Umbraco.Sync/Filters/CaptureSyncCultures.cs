using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using System.Linq;
using uSync.Expansions.Core;

namespace N3O.Umbraco.Sync;

public class CaptureSyncCultures : IActionFilter {
    private readonly ILogger<CaptureSyncCultures> _logger;

    public CaptureSyncCultures(ILogger<CaptureSyncCultures> logger) {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context) {
        var cultures = context.ActionArguments
                              .Values
                              .OfType<SyncPackRequestItemsBase>()
                              .FirstOrDefault()?
                              .Options?
                              .Cultures;

        if (cultures.HasAny()) {
            context.HttpContext.Items[SyncConstants.HttpContextItems.CultureScopedPush] = true;

            _logger.LogDebug("Culture scoped sync request {Action} for {Cultures}",
                             context.ActionDescriptor.DisplayName,
                             string.Join(", ", cultures));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
