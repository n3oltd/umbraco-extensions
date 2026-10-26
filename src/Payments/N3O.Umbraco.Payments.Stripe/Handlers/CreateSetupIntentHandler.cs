using N3O.Umbraco.Content;
using N3O.Umbraco.Payments.Handlers;
using N3O.Umbraco.Payments.Models;
using N3O.Umbraco.Payments.Stripe.Commands;
using N3O.Umbraco.Payments.Stripe.Models;
using Stripe;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Extensions;

namespace N3O.Umbraco.Payments.Stripe.Handlers;

public class CreateSetupIntentHandler :
    PaymentsHandler<CreateSetupIntentCommand, SetupIntentReq, StripeCredential> {
    private readonly IContentCache _contentCache;
    private readonly StripeClient _stripeClient;
    private readonly ICustomers _customers;

    public CreateSetupIntentHandler(IPaymentsScope paymentsScope,
                                    IContentCache contentCache,
                                    StripeClient stripeClient,
                                    ICustomers customers)
        : base(paymentsScope) {
        _contentCache = contentCache;
        _stripeClient = stripeClient;
        _customers = customers;
    }

    protected override async Task HandleAsync(CreateSetupIntentCommand req,
                                              StripeCredential credential,
                                              PaymentsParameters parameters,
                                              CancellationToken cancellationToken) {
        try {
            var settings = _contentCache.Single<StripeSettingsContent>();
            var billingInfo = parameters.BillingInfoAccessor.GetBillingInfo();
            var customer = await _customers.CreateCustomerAsync(billingInfo);
            var service = new SetupIntentService(_stripeClient);
            var setupIntentOptions = GetSetupIntentOptions(parameters,req.Model, customer);
            
            var options = new RequestOptions();
            options.IdempotencyKey = parameters.GetTransactionId(settings, req.Model.PaymentMethodId);
            
            var setupIntent = await service.CreateAsync(setupIntentOptions, options, cancellationToken);
            
            credential.IntentCreated(setupIntent);
        } catch (StripeException ex) {
            credential.Error(ex);
        }
    }
    
    private SetupIntentCreateOptions GetSetupIntentOptions(PaymentsParameters parameters,
                                                           SetupIntentReq req,
                                                           Customer customer) {
        var settings = _contentCache.Single<StripeSettingsContent>();
        
        var options = new SetupIntentCreateOptions();
        options.Confirm = true;
        options.AllowedPaymentMethodTypes = "card".Yield().ToList();
        options.Customer = customer.Id;
        options.Usage = "off_session";
        options.Description = parameters.GetTransactionDescription(settings);
        options.PaymentMethod = req.PaymentMethodId;
        options.Confirm = true;

        return options;
    }
}
