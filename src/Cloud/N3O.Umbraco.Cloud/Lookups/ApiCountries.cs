using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Lookups;

[Order(int.MaxValue)]
public class ApiCountries : ApiLookupsCollection<Country> {
    private readonly ICdnClient _cdnClient;

    public ApiCountries(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }
    
    protected override async Task<IReadOnlyList<Country>> FetchAsync(CancellationToken cancellationToken) {
        var publishedLookups = await _cdnClient.DownloadSubscriptionContentAsync<PublishedLookupsRoot>(SubscriptionFiles.Lookups,
                                                                                                       JsonSerializers.Simple,
                                                                                                       cancellationToken);

        var countries = new List<Country>();

        foreach (var publishedCountry in publishedLookups.OrEmpty(x => x.Lookups?.Countries?.Items)) {
            var country = new Country(publishedCountry.Id,
                                      publishedCountry.Name,
                                      null,
                                      publishedCountry.Iso2Code,
                                      publishedCountry.Iso3Code,
                                      publishedCountry.DialingCode,
                                      publishedCountry.LocalityOptional,
                                      publishedCountry.PostalCodeOptional);
            
            countries.Add(country);
        }

        return countries;
    }

    protected override TimeSpan ReloadInterval => TimeSpan.FromMinutes(1);
}