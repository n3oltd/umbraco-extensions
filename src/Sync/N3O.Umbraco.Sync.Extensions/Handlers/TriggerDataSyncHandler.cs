using Flurl;
using N3O.Umbraco.Json;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Scheduler.Attributes;
using N3O.Umbraco.Sync.Extensions.Attributes;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using N3O.Umbraco.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Sync.Extensions.Handlers;

[RecurringJob("Trigger Data Sync", "*/5 * * * *")]
public class TriggerDataSyncHandler : IRequestHandler<TriggerDataSyncCommand, None, None> {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonProvider _jsonProvider;
    private readonly IUrlBuilder _urlBuilder;
    private readonly IReadOnlyList<IDataSyncProducer> _producers;

    public TriggerDataSyncHandler(IEnumerable<IDataSyncProducer> producers,
                                  IHttpClientFactory httpClientFactory,
                                  IJsonProvider jsonProvider,
                                  IUrlBuilder urlBuilder) {
        _httpClientFactory = httpClientFactory;
        _producers = producers.ToList();
        _jsonProvider = jsonProvider;
        _urlBuilder = urlBuilder;
    }
    
    public async Task<None> Handle(TriggerDataSyncCommand req, CancellationToken cancellationToken) {
        foreach (var producer in _producers) {
            var syncRegistration = GetSyncRegistration(producer);

            if (syncRegistration != null && syncRegistration.IsDue()) {
                var allData = await producer.ProvideAsync();
            
                foreach (var data in allData) {
                    var syncDataReq = GetSyncDataReq(syncRegistration.SharedSecret, data);
                
                    await SyncDataAsync(syncDataReq, syncRegistration.ProviderId);
                }
            
                syncRegistration.MarkSynchronized();
            }
        }

        return None.Empty;
    }

    private SyncDataReq GetSyncDataReq(string sharedSecret, object data) {
        var req = new SyncDataReq();
        req.Data =  data;
        req.SharedSecret = sharedSecret;
        
        return req;
    }
    
    private SyncRegistration GetSyncRegistration(IDataSyncProducer producer) {
        var providerId = producer.GetType().GetCustomAttribute<DataSyncProviderAttribute>()?.Id;

        return DataSync.GetRegistrationByProviderId(providerId);
    }
    
    private async Task SyncDataAsync(SyncDataReq syncDataReq, string providerId) {
        var url = _urlBuilder.Root()
                             .AppendPathSegments("umbraco", "api", "SyncExtensions", providerId, "syncData");
        var httpClient = _httpClientFactory.CreateClient();
        var reqStr = _jsonProvider.SerializeObject(syncDataReq);

        var request = new HttpRequestMessage(HttpMethod.Post, url.ToUri());
        request.Content = new StringContent(reqStr, null, "application/json");
        request.Headers.Add("accept", "*/*");

        var response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}