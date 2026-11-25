using N3O.Umbraco.Cloud.Lookups;

namespace N3O.Umbraco.Cloud;

public interface ICloudUrl {
    string ForApi(CloudApiType type, string servicePath);
    string ForCdn(CdnRoot root, string path);
    string ForConnectApi(string servicePath);
    string ForEngageApi(string servicePath);
    string ForLinkBuilder(string path);
    string ForPlatforms(string path);
    string ForWebhook(string webhookId);

    string ConnectApiBaseUrl { get; }
}