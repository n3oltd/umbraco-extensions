using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Accounts.Models;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Checkout.Commands;
using N3O.Umbraco.Giving.Checkout.Lookups;
using N3O.Umbraco.Giving.Checkout.Models;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

[ApiDocument(CheckoutConstants.ApiName)]
public class CheckoutController : LookupsController<CheckoutLookupsRes> {
    private readonly IMediator _mediator;

    public CheckoutController(IMediator mediator, ILookups lookups, IUmbracoMapper mapper) : base(lookups, mapper) {
        _mediator = mediator;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CheckoutRes>> GetCurrentCheckout() {
        var res = await _mediator.SendAsync<GetOrBeginCheckoutCommand, None, CheckoutRes>(None.Empty);
         
        return Ok(res);
    }

    [HttpGet("lookups/" + CheckoutLookupTypes.CheckoutStages)]
    public async Task<ActionResult<IEnumerable<NamedLookupRes>>> GetLookupCheckoutStages() {
        var res = await GetLookupsAsync<CheckoutStage>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + CheckoutLookupTypes.RegularGivingFrequencies)]
    public async Task<ActionResult<IEnumerable<NamedLookupRes>>> GetRegularGivingFrequencies() {
        var res = await GetLookupsAsync<RegularGivingFrequency>();

        return Ok(res);
    }
    
    [HttpPost("{checkoutRevisionId:revisionId}/account")]
    public async Task<ActionResult<CheckoutRes>> UpdateAccount(AccountReq req) {
        var res = await _mediator.SendAsync<UpdateAccountCommand, AccountReq, CheckoutRes>(req);
         
        return Ok(res);
    }
    
    [HttpPost("{checkoutRevisionId:revisionId}/account/consent")]
    public async Task<ActionResult<CheckoutRes>> UpdateAccountConsent(ConsentReq req) {
        var res = await _mediator.SendAsync<UpdateAccountConsentCommand, ConsentReq, CheckoutRes>(req);
         
        return Ok(res);
    }
    
    [HttpPost("{checkoutRevisionId:revisionId}/account/taxStatus")]
    public async Task<ActionResult<CheckoutRes>> UpdateAccountTaxStatus(TaxStatusReq req) {
        var res = await _mediator.SendAsync<UpdateAccountTaxStatusCommand, TaxStatusReq, CheckoutRes>(req);
         
        return Ok(res);
    }
    
    [HttpPost("{checkoutRevisionId:revisionId}/regularGiving/options")]
    public async Task<ActionResult<CheckoutRes>> UpdateRegularGivingOptions(RegularGivingOptionsReq req) {
        var res = await _mediator.SendAsync<UpdateRegularGivingOptionsCommand, RegularGivingOptionsReq, CheckoutRes>(req);

        return Ok(res);
    }
}
