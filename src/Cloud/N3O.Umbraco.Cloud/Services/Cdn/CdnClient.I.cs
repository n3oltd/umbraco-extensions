using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud;

public interface ICdnClient {
    Task<string> DownloadAsync(string path, CancellationToken cancellationToken = default);
    
    Task<PublishedContentResult<T>> DownloadPublishedContentAsync<T>(PublishedFileKind kind,
                                                                     string path,
                                                                     JsonSerializer jsonSerializer,
                                                                     CancellationToken cancellationToken = default);
    
    Task<PublishedContentResult> DownloadPublishedContentAsync(string path,
                                                               CancellationToken cancellationToken = default);

    // Eviction applies to the calling process only, and reaches only entries fetched through the
    // matching overload.
    void Evict(string path);

    void Evict(PublishedFileKind kind, string path);
}