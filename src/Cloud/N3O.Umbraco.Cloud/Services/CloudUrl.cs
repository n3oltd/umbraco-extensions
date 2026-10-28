using Flurl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Dev;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Cloud;

public class CloudUrl : ICloudUrl {
    private readonly SubscriptionInfo _subscription;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CloudUrl(ISubscriptionAccessor subscriptionAccessor, IWebHostEnvironment webHostEnvironment) {
        _subscription = subscriptionAccessor.GetSubscription();
        _webHostEnvironment = webHostEnvironment;
    }
    
    public string ForApi(CloudApiType type, string servicePath) {
        if (type == CloudApiTypes.Connect) {
            return ForConnectApi(servicePath);
        } else if (type == CloudApiTypes.Engage) {
            return ForEngageApi(servicePath);
        } else {
            throw UnrecognisedValueException.For(type);
        }
    }
    
    public string ForCdn(CdnRoot root, string path) {
        var url = GetCdnBaseUrl();
        
        url.AppendPathSegment($"{root.Container.Prefix}-{_subscription.Id.Code}");

        if (root.PathSegment.HasValue()) {
            url.AppendPathSegment(root.PathSegment);
        }
        
        url.AppendPathSegment(path);

        return url;
    }

    public string ForConnectApi(string servicePath) {
        return ForRegionalApi(servicePath);
    }

    public string ForEngageApi(string servicePath) {
        return ForRegionalApi(servicePath);
    }

    public string ForLinkBuilder(string path) {
        var url = new Url("https://linkbuilder.n3o.cloud");
        url.AppendPathSegment(_subscription.DataRegion.Slug);
        url.AppendPathSegment(path);

        return url;
    }

    public string ForWebhook(string webhookId) {
        var url = new Url(GetRegionalApiBaseUrl());
        url.AppendPathSegment("hooks");
        url.AppendPathSegment($"{webhookId}/{_subscription.Id.Code}");

        return url;
    }

    public string ConnectApiBaseUrl => new Url(GetRegionalApiBaseUrl()).AppendPathSegment("api");

    private string ForRegionalApi(string servicePath) {
        var url = new Url(GetRegionalApiBaseUrl());
        // Clients generated against the legacy hosts carry "/eu1/api/<alias>" as their base URL, those
        // generated against the regional host carry "/api/<alias>"
        url.AppendPathSegment(servicePath.Replace("eu1/api", "api"));

        return url;
    }

    private string GetRegionalApiBaseUrl() {
        if (_webHostEnvironment.IsDevelopment() && !DevFlags.IsSet(DevFlags.EnableLiveTesting)) {
            return $"https://{_subscription.DataRegion.Slug}.qa.n3o.cloud";
        } else {
            return $"https://{_subscription.DataRegion.Slug}.n3o.cloud";
        }
    }

    private Url GetCdnBaseUrl() {
        if (_webHostEnvironment.IsDevelopment() && !DevFlags.IsSet(DevFlags.EnableLiveTesting)) {
            return new Url("https://cdn.qa.n3o.cloud");
        } else {
            return new Url("https://cdn.n3o.cloud");
        }
    }
}