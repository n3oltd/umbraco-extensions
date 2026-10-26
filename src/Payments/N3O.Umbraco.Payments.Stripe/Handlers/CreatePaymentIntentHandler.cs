using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
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

public class CreatePaymentIntentHandler :
    PaymentsHandler<CreatePaymentIntentCommand, PaymentIntentReq, StripePayment> {
    private readonly IContentCache _contentCache;
    private readonly StripeClient _stripeClient;
    private readonly ICustomers _customers;

    public CreatePaymentIntentHandler(IPaymentsScope paymentsScope,
                                      IContentCache contentCache,
                                      StripeClient stripeClient,
                                      ICustomers customers)
        : base(paymentsScope) {
        _contentCache = contentCache;
        _stripeClient = stripeClient;
        _customers = customers;
    }

    protected override async Task HandleAsync(CreatePaymentIntentCommand req,
                                              StripePayment payment,
                                              PaymentsParameters parameters,
                                              CancellationToken cancellationToken) {
        try {
            var settings = _contentCache.Single<StripeSettingsContent>();
            var billingInfo = parameters.BillingInfoAccessor.GetBillingInfo();
            var customer = await _customers.CreateCustomerAsync(billingInfo);
            var service = new PaymentIntentService(_stripeClient);
            var value = parameters.GetPaymentAmount(payment.Type);

            var paymentIntentOptions = GetPaymentIntentOptions(parameters, req.Model, customer, value);

            var options = new RequestOptions();
            options.IdempotencyKey = parameters.GetTransactionId(settings, req.Model.PaymentMethodId);

            var paymentIntent = await service.CreateAsync(paymentIntentOptions, options, cancellationToken);

            payment.IntentCreated(paymentIntent);
        } catch (StripeException ex) {
            payment.Error(ex);
        }
    }
    
    private PaymentIntentCreateOptions GetPaymentIntentOptions(PaymentsParameters parameters,
                                                               PaymentIntentReq req,
                                                               Customer customer,
                                                               Money value) {
        var settings = _contentCache.Single<StripeSettingsContent>();
        var options = new PaymentIntentCreateOptions();

        options.Amount = value.GetAmountInLowestDenomination();
        options.Currency = value.Currency.Code;
        options.Description = parameters.GetTransactionDescription(settings);
        options.ErrorOnRequiresAction = false;
        options.Customer = customer.Id;
        options.PaymentMethod = req.PaymentMethodId;
        options.Confirm = true;
        options.ConfirmationMethod = "manual";
        options.CaptureMethod = "automatic";
        options.AllowedPaymentMethodTypes = "card".Yield().ToList();

        return options;
    }
}
