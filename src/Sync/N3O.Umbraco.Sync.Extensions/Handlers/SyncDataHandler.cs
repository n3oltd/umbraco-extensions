using N3O.Umbraco.Extensions;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Sync.Extensions.Handlers;

public class SyncDataHandler : IRequestHandler<SyncDataCommand, SyncDataReq, None> {
    private readonly IReadOnlyList<IDataSyncConsumer> _consumers;
    
    public SyncDataHandler(IEnumerable<IDataSyncConsumer> consumers) {
        _consumers = consumers.ToList();
    }
    
    public async Task<None> Handle(SyncDataCommand req, CancellationToken cancellationToken) {
        var providerRegistration = DataSync.GetRegistrationByProviderId(req.ProviderId);

        if (!providerRegistration.HasValue()) {
            return None.Empty;
        }

        if (!providerRegistration.SharedSecret.EqualsSecret(req.Model.SharedSecret)) {
            throw new UnauthorizedAccessException();
        }

        var consumer = GetConsumer(providerRegistration.ConsumerType);
        
        if (consumer == null) {
            return None.Empty;
        }

        await consumer.ConsumeAsync(req.Model.Data);
        
        return None.Empty;
    }

    private IDataSyncConsumer GetConsumer(Type consumerType) {
        return _consumers.SingleOrDefault(x => x.GetType() == consumerType);
    }
}