using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Authentication.Auth0;
using N3O.Umbraco.Authentication.Auth0.Lookups;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

[Order(int.MaxValue)]
public class ApiFundraisers : ApiLookupsCollection<Fundraiser> {
    private readonly ClientFactory<AnalyticsDataEntryClient> _clientFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ApiFundraisers(ClientFactory<AnalyticsDataEntryClient> clientFactory,
                          IServiceScopeFactory serviceScopeFactory) {
        _clientFactory = clientFactory;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task<IReadOnlyList<Fundraiser>> FetchAsync(CancellationToken cancellationToken) {
        using (var scope = _serviceScopeFactory.CreateScope()) {
            var auth0TokenAccessor = scope.ServiceProvider.GetRequiredService<Auth0TokenAccessor>();
            var bearerToken = await auth0TokenAccessor.GetAsync(UserDirectoryTypes.BackOffice);
            var client = _clientFactory.Create(CloudApiTypes.Engage, bearerToken);

            var res = await client.InvokeAsync(x => x.ListUnscopedFundraisersAsync(cancellationToken));

            var fundraisers = new List<Fundraiser>();

            foreach (var fundraiserRes in res.OrEmpty(x => x.Items)) {
                var fundraiser = new Fundraiser(fundraiserRes.Id, fundraiserRes.Name);

                fundraisers.Add(fundraiser);
            }

            return fundraisers;
        }
    }
}
