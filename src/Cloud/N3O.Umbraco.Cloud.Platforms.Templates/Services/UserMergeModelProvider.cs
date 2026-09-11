using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Templates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class UserMergeModelProvider : MergeModelsProvider {
    private readonly ILogger<UserMergeModelProvider> _logger;
    private readonly UserCookie _userCookie;
    private readonly Lazy<ClientFactory<PlatformsConnectClient>> _clientFactory;

    public UserMergeModelProvider(ILogger<UserMergeModelProvider> logger,
                                  UserCookie userCookie,
                                  Lazy<ClientFactory<PlatformsConnectClient>> clientFactory) {
        _logger = logger;
        _userCookie = userCookie;
        _clientFactory = clientFactory;
    }
    
    public override Task<bool> IsProviderForAsync(IPublishedContent content) => Task.FromResult(true);

    protected override async Task PopulateModelsAsync(IPublishedContent content,
                                                      Dictionary<string, object> mergeModels,
                                                      CancellationToken cancellationToken = default) {
        try {
            var bearerToken = _userCookie.GetValue();

            if (bearerToken.HasValue()) {
                var client = _clientFactory.Value.Create(CloudApiTypes.Connect, bearerToken);

                var platformsUser = await client.InvokeAsync(x => x.GetPlatformsUserAsync(cancellationToken));

                if (platformsUser.HasValue()) {
                    mergeModels["user"] = platformsUser;
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Error fetching platforms user: {Error}", ex.Message);;
        }
    }
}