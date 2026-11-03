using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Giving.Commands;
using N3O.Umbraco.Giving.Queries;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using AllocationType = N3O.Umbraco.Giving.Allocations.Lookups.AllocationType;
using CurrencyRes = N3O.Umbraco.Financial.CurrencyRes;
using DonationFormRes = N3O.Umbraco.Giving.Allocations.Models.DonationFormRes;
using DonationItemRes = N3O.Umbraco.Giving.Allocations.Models.DonationItemRes;
using DonationPlan = N3O.Umbraco.Giving.Allocations.Lookups.DonationPlan;
using DonationPlanRes = N3O.Umbraco.Giving.Allocations.Models.DonationPlanRes;
using FeedbackSchemeRes = N3O.Umbraco.Giving.Allocations.Models.FeedbackSchemeRes;
using FundStructureRes = N3O.Umbraco.Giving.Allocations.Models.FundStructureRes;
using GivingLookupsRes = N3O.Umbraco.Giving.Models.GivingLookupsRes;
using GivingType = N3O.Umbraco.Giving.Allocations.Lookups.GivingType;
using NamedLookupRes = N3O.Umbraco.Lookups.NamedLookupRes;
using PriceCriteria = N3O.Umbraco.Giving.Allocations.Models.PriceCriteria;
using PriceRes = N3O.Umbraco.Giving.Allocations.Models.PriceRes;
using SponsorshipDuration = N3O.Umbraco.Giving.Allocations.Lookups.SponsorshipDuration;
using SponsorshipDurationRes = N3O.Umbraco.Giving.Allocations.Models.SponsorshipDurationRes;
using SponsorshipSchemeRes = N3O.Umbraco.Giving.Allocations.Models.SponsorshipSchemeRes;

namespace N3O.Umbraco.Giving.Controller;

[ApiDocument(GivingConstants.ApiName)]
public class GivingController : LookupsController<GivingLookupsRes> {
    private readonly IMediator _mediator;

    public GivingController(ILookups lookups, IUmbracoMapper mapper, IMediator mediator)
        : base(lookups, mapper) {
        _mediator = mediator;
    }
    
    [HttpGet("donationForms/{donationFormId:guid}")]
    public async Task<ActionResult<DonationFormRes>> GetDonationForm() {
        try {
            var res = await _mediator.SendAsync<GetDonationFormByIdQuery, None, DonationFormRes>(None.Empty);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }

    [HttpGet("fundStructure")]
    public async Task<ActionResult<FundStructureRes>> GetFundStructure() {
        var res = await _mediator.SendAsync<GetFundStructureQuery, None, FundStructureRes>(None.Empty);

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.AllocationTypes)]
    public async Task<ActionResult<IEnumerable<NamedLookupRes>>> GetLookupAllocationTypes() {
        var res = await GetLookupsAsync<AllocationType>();

        return Ok(res);
    }

    [HttpGet("lookups/" + GivingLookupTypes.Currencies)]
    public async Task<ActionResult<IEnumerable<CurrencyRes>>> GetLookupCurrencies() {
        var res = await GetLookupsAsync<Currency, CurrencyRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.DonationItems)]
    public async Task<ActionResult<IEnumerable<DonationItemRes>>> GetLookupDonationItems() {
        var res = await GetLookupsAsync<DonationItem, DonationItemRes>();

        return Ok(res);
    }

    [HttpGet("lookups/" + GivingLookupTypes.DonationPlans)]
    public async Task<ActionResult<IEnumerable<DonationPlanRes>>> GetLookupDonationPlans() {
        var res = await GetLookupsAsync<DonationPlan, DonationPlanRes>();

        return Ok(res);
    }

    [HttpGet("lookups/" + GivingLookupTypes.FeedbackSchemes)]
    public async Task<ActionResult<IEnumerable<FeedbackSchemeRes>>> GetLookupFeedbackSchemes() {
        var res = await GetLookupsAsync<FeedbackScheme, FeedbackSchemeRes>();

        return Ok(res);
    }

    [HttpGet("lookups/" + GivingLookupTypes.FundDimension1Values)]
    public async Task<ActionResult<IEnumerable<FundDimensionValueRes>>> GetLookupFundDimension1Values() {
        var res = await GetLookupsAsync<FundDimension1Value, FundDimensionValueRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.FundDimension2Values)]
    public async Task<ActionResult<IEnumerable<FundDimensionValueRes>>> GetLookupFundDimension2Values() {
        var res = await GetLookupsAsync<FundDimension2Value, FundDimensionValueRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.FundDimension3Values)]
    public async Task<ActionResult<IEnumerable<FundDimensionValueRes>>> GetLookupFundDimension3Values() {
        var res = await GetLookupsAsync<FundDimension3Value, FundDimensionValueRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.FundDimension4Values)]
    public async Task<ActionResult<IEnumerable<FundDimensionValueRes>>> GetLookupFundDimension4Values() {
        var res = await GetLookupsAsync<FundDimension4Value, FundDimensionValueRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.GivingTypes)]
    public async Task<ActionResult<IEnumerable<NamedLookupRes>>> GetLookupGivingTypes() {
        var res = await GetLookupsAsync<GivingType>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.SponsorshipDurations)]
    public async Task<ActionResult<IEnumerable<SponsorshipDurationRes>>> GetLookupSponsorshipDurations() {
        var res = await GetLookupsAsync<SponsorshipDuration, SponsorshipDurationRes>();

        return Ok(res);
    }
    
    [HttpGet("lookups/" + GivingLookupTypes.SponsorshipSchemes)]
    public async Task<ActionResult<IEnumerable<SponsorshipSchemeRes>>> GetLookupSponsorshipSchemes() {
        var res = await GetLookupsAsync<SponsorshipScheme, SponsorshipSchemeRes>();

        return Ok(res);
    }

    [HttpPost("pricing")]
    public async Task<ActionResult<PriceRes>> GetPrice(PriceCriteria criteria) {
        var res = await _mediator.SendAsync<GetPriceQuery, PriceCriteria, PriceRes>(criteria);

        return Ok(res);
    }

    [HttpPost("currency/{currencyCode}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrencyRes>> SetCurrency() {
        try {
            var res = await _mediator.SendAsync<SetCurrencyCommand, None, CurrencyRes>(None.Empty);

            return Ok(res);
        } catch (ResourceNotFoundException ex) {
            return NotFound(ex);
        }
    }
}
