using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Humanizer.Bytes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Storage.Azure;

public class AzureStorageFolder : IStorageFolder {
    private readonly BlobContainerClient _container;
    private readonly string _folderPath;

    public AzureStorageFolder(BlobContainerClient container, string folderPath) {
        _container = container;
        _folderPath = folderPath;
    }
    
    public async Task AddFileAsync(string filename, Stream stream) {
        var blobClient = _container.GetBlobClient(GetBlobName(filename));

        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task AddFileAsync(string name, byte[] contents) {
        using (var stream = new MemoryStream(contents)) {
            await AddFileAsync(name, stream);
        }
    }

    public async Task DeleteAllFilesAsync() {
        var options = new GetBlobsOptions();
        options.Prefix = _folderPath;

        var result = _container.GetBlobsAsync(options);

        await foreach (var page in result.AsPages()) {
            foreach (var blob in page.Values) {
                await _container.DeleteBlobAsync(blob.Name);
            }
        }
    }

    public async Task DeleteFileAsync(string name) {
        var blobClient = await GetBlobClientAsync(name);

        await blobClient.DeleteAsync();
    }

    public async Task<Blob> GetFileAsync(string filename, CancellationToken cancellationToken = default) {
        var blobClient = await GetBlobClientAsync(filename);
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        return new Blob(filename,
                        _folderPath,
                        FileUtility.GetContentType(filename),
                        ByteSize.FromBytes(properties.Value.ContentLength),
                        await blobClient.OpenReadAsync(cancellationToken: cancellationToken));
    }

    private async Task<BlobClient> GetBlobClientAsync(string filename) {
        var blobClient = _container.GetBlobClient(GetBlobName(filename));
        var exists = await blobClient.ExistsAsync();

        if (!exists) {
            throw new FileNotFoundException($"File {filename.Quote()} does not exist in folder {_folderPath.Quote()}");
        }

        return blobClient;
    }

    private string GetBlobName(string filename) {
        return $"{_folderPath}/{filename}";
    }
}
