using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Giving.Checkout.Commands;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
using System.Threading.Tasks;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

[ApiDocument(CheckoutConstants.DevToolsApiName)]
public class CheckoutDevToolsController : BackofficeAuthorizedApiController {
    private readonly IMediator _mediator;

    public CheckoutDevToolsController(IMediator mediator) {
        _mediator = mediator;
    }

    [HttpPost("{checkoutRevisionId:revisionId}/webhooks/resend")]
    public async Task<ActionResult> ResendCheckoutWebhook() {
        var res = await _mediator.SendAsync<ResendCheckoutWebhookCommand, None, None>(None.Empty);

        return Ok(res);
    }
}
