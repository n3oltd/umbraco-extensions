using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Payments.PayPal.Clients;
using N3O.Umbraco.Payments.PayPal.Clients.Models;
using N3O.Umbraco.Payments.PayPal.Commands;
using Newtonsoft.Json;
using Refit;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Handlers;

public class GetOrCreatePlanHandler : IRequestHandler<GetOrCreatePlanCommand, MoneyReq, string> {
    private readonly IPlansClient _plansClient;
    private readonly IProductsClient _productsClient;

    public GetOrCreatePlanHandler(IPlansClient plansClient, IProductsClient productsClient) {
        _plansClient = plansClient;
        _productsClient = productsClient;
    }
    
    public async Task<string> Handle(GetOrCreatePlanCommand req, CancellationToken cancellationToken) {
        var planName = $"{req.Model.Currency.Symbol}/{req.Model.Amount:0.00}";

        var productId = await GetOrCreateProductAsync();
        var planId = await GetOrCreatePlanAsync(productId, planName, req.Model);
        
        return planId;
    }
    
    private async Task<string> GetOrCreateProductAsync() {
        string productId;
        
        var apiReq = new ApiCreateProductReq();
        apiReq.Id = PayPalConstants.ProductId;
        apiReq.Name = PayPalConstants.ProductId;
        apiReq.Category = PayPalConstants.ProductCategory;
        apiReq.Type = PayPalConstants.ProductType;
        apiReq.Category = PayPalConstants.ProductCategory;
        
        try {
            var res = await _productsClient.CreateProductAsync(apiReq);
            
            productId = res.Id;
        } catch (ApiException apiException) {
            var paypalError = apiException.Content.IfNotNull(JsonConvert.DeserializeObject<PayPalError>);

            var issue = paypalError?.Details[0]?.Issue;

            if (issue != null && issue == "DUPLICATE_RESOURCE_IDENTIFIER") {
                productId = apiReq.Id;
            } else {
                throw;
            }
        }
        
        return productId;
    }

    private async Task<string> GetOrCreatePlanAsync(string productId, string planName, Money value) {
        var planId = default(string);
        
        for (var page = 1; true; page++) {
            var apiReq = new ApiGetPlansReq();
            apiReq.ProductId = productId;
            apiReq.PageNumber = page.ToString();
            apiReq.TotalRequired = true;
            
            var apiRes = await _plansClient.GetPlansAsync(apiReq);
            
            if (apiRes.TotalItems > 0) {
                var plan = apiRes.Plans.FirstOrDefault(x => x.Status == PayPalConstants.PlanStatus &&
                                                            x.Name.EqualsInvariant(planName));

                planId = plan?.Id;
            }

            if (planId.HasValue() || page == apiRes.TotalPages) {
                break;
            }
        }
        
        planId ??= await CreatePlanAsync(productId, planName, value);
        
        return planId;
    }
    
    private async Task<string> CreatePlanAsync(string productId, string planName, Money value) {
        var fixedPrice = new FixedPrice();
        fixedPrice.Value = value.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        fixedPrice.CurrencyCode = value.Currency.Code;
        
        var pricingScheme = new PricingScheme();
        pricingScheme.FixedPrice = fixedPrice;
        
        var frequency = new Frequency();
        frequency.IntervalCount = PayPalConstants.FrequencyCount;
        frequency.IntervalUnit = PayPalConstants.FrequencyInterval;
        
        var billingCycle = new BillingCycle();
        billingCycle.Frequency = frequency;
        billingCycle.TenureType = PayPalConstants.TenureType;
        billingCycle.Sequence = PayPalConstants.BillingCycleSequence;
        billingCycle.TotalCycles = PayPalConstants.BillingCycleTotalCycles;
        billingCycle.PricingScheme = pricingScheme;
        
        var paymentPreferences = new PaymentPreferences();
        paymentPreferences.AutoBillOutstanding = true;
        paymentPreferences.SetupFeeFailureAction = PayPalConstants.SetupFeeFailureAction;
        paymentPreferences.PaymentFailureThreshold = PayPalConstants.PaymentFailureThreshold;
        
        var apiReq = new ApiCreatePlanReq();
        apiReq.ProductId = productId;
        apiReq.Name = planName;
        apiReq.Status = PayPalConstants.PlanStatus;
        apiReq.BillingCycles = [billingCycle];
        apiReq.PaymentPreferences = paymentPreferences;

        try {
            var res = await _plansClient.CreatePlanAsync(apiReq);
            
            return res.Id;
        } catch (ApiExceptionBase apiException) when (apiException is ApiException ||
                                                      apiException.InnerException is ApiException) {
            return "";
        }
    }
}