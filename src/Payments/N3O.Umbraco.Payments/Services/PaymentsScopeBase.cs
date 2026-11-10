using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Payments.Entities;
using N3O.Umbraco.Payments.Lookups;
using N3O.Umbraco.Payments.Models;
using NodaTime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments;

public abstract class PaymentsScopeBase : IPaymentsScope {
    private readonly IClock _clock;
    private readonly IFormatter _formatter;
    private readonly ILogger<PaymentsScopeBase> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    protected PaymentsScopeBase(IClock clock,
                                IFormatter formatter,
                                ILogger<PaymentsScopeBase> logger,
                                IWebHostEnvironment webHostEnvironment) {
        _clock = clock;
        _formatter = formatter;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public async Task<PaymentFlowRes<T>> DoAsync<T>(Func<IPaymentsFlow, T, Task> actionAsync,
                                                    CancellationToken cancellationToken = default)
        where T : PaymentObject, new() {
        var flow = await LoadAsync(cancellationToken);
        
        T paymentObject;
        if (typeof(T).IsSubclassOfType(typeof(Payment))) {
            paymentObject = GetOrCreate<T>(flow, PaymentObjectTypes.Payment);
        } else if (typeof(T).IsSubclassOfType(typeof(Credential))) {
            paymentObject = GetOrCreate<T>(flow, PaymentObjectTypes.Credential);
        } else {
            throw new NotImplementedException();
        }

        if (_webHostEnvironment.IsProduction()) {
            paymentObject.ClearExceptionDetails();
        }

        try {
            flow.BeginPaymentFlow(_clock);

            await actionAsync(flow, paymentObject);
            
            flow.EndPaymentFlow();
        } catch (Exception ex) {
            _logger.LogError(ex, "Unhandled error in payments flow {FlowId}", flow.Id);

            var message = _formatter.Text.Format<UnhandledErrorStrings>(s => s.Message_1, flow.Id);
            var exceptionDetails = _webHostEnvironment.IsProduction() ? null : ex.ToString();

            paymentObject.UnhandledError(exceptionDetails, message);
        }

        await UpdateAsync(flow, cancellationToken);

        return new PaymentFlowRes<T>(flow, paymentObject);
    }

    protected abstract Task<IPaymentsFlow> LoadAsync(CancellationToken cancellationToken);
    protected abstract Task UpdateAsync(IPaymentsFlow flow, CancellationToken cancellationToken);

    private T GetOrCreate<T>(IPaymentsFlow flow, PaymentObjectType paymentObjectType)
        where T : PaymentObject, new() {
        var existing = flow.GetPaymentObject(paymentObjectType);

        if (existing is T paymentObject) {
            return paymentObject;
        } else {
            if (existing?.Status == PaymentObjectStatuses.Complete) {
                throw new Exception("Cannot change payment method at this time");
            }
            
            var newPaymentObject = new T();
            flow.SetPaymentObject(paymentObjectType, newPaymentObject);

            return newPaymentObject;
        }
    }
}
