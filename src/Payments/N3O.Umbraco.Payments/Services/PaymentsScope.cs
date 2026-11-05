using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Entities;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Payments.Entities;
using N3O.Umbraco.Payments.NamedParameters;
using NodaTime;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments;

public class PaymentsScope : PaymentsScopeBase {
    private readonly IRepository<IPaymentsFlow> _repository;
    private readonly FlowId _flowId;
    private IPaymentsFlow _flow;

    public PaymentsScope(IClock clock,
                         IFormatter formatter,
                         ILogger<PaymentsScopeBase> logger,
                         IWebHostEnvironment webHostEnvironment,
                         IRepository<IPaymentsFlow> repository,
                         FlowId flowId)
        : base(clock, formatter, logger, webHostEnvironment) {
        _repository = repository;
        _flowId = flowId;
    }

    protected override async Task<IPaymentsFlow> LoadAsync(CancellationToken cancellationToken) {
        _flow ??= await _flowId.RunAsync(_repository.GetAsync, true, cancellationToken);

        return _flow;
    }

    protected override async Task UpdateAsync(IPaymentsFlow flow, CancellationToken cancellationToken) {
        await _repository.UpdateAsync(flow);
    }
}
