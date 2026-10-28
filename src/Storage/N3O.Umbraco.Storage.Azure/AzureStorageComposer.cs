using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using SixLabors.ImageSharp.Web.Caching;
using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.ImageSharp;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace N3O.Umbraco.Storage.Azure;

[ComposeAfter(typeof(StorageComposer))]
public class AzureStorageComposer : Composer {
    private const string DataKeysXml = "datakeys.xml";
    private const string ImageCacheRootPath = "cache";

    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IVolume, AzureVolume>();

        var storageConfigured = builder.Config
                                       .GetSection("umbraco")
                                       .GetChildren()
                                       .Any(x => x.Key.EqualsInvariant("storage") &&
                                                 x.GetChildren().Any(c => c.Key.EqualsInvariant("azureBlob")));

        if (!storageConfigured) {
            return;
        }

        if (AzureBlobStorage.UsesIdentity(builder.Config)) {
            ComposeWithIdentity(builder);
        } else {
            ComposeWithConnectionString(builder);
        }
    }

    private void ComposeWithIdentity(IUmbracoBuilder builder) {
        var container = AzureBlobStorage.GetMediaContainerClient(builder.Config);

        // Umbraco does not register a content type provider; the stock Azure provider
        // constructs its own, so the credential path must supply one the same way.
        builder.Services.TryAddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();

        builder.SetMediaFileSystem(provider => {
            var hostingEnvironment = provider.GetRequiredService<IHostingEnvironment>();
            var globalSettings = provider.GetRequiredService<IOptions<GlobalSettings>>();
            var requestRootPath = hostingEnvironment.ToAbsolute(globalSettings.Value.UmbracoMediaPath);

            return new AzureBlobFileSystem(requestRootPath,
                                           container,
                                           provider.GetRequiredService<IIOHelper>(),
                                           provider.GetRequiredService<IContentTypeProvider>(),
                                           containerRootPath: null,
                                           hybridCache: null,
                                           cacheOptions: null);
        });

        builder.Services.AddUnique<IImageCache>(_ => new AzureBlobFileSystemImageCache(container,
                                                                                       ImageCacheRootPath));

        builder.Services
               .AddDataProtection()
               .PersistKeysToAzureBlobStorage(AzureBlobStorage.GetBlobUri(builder.Config, DataKeysXml),
                                              AzureBlobStorage.GetCredential());
    }

    private void ComposeWithConnectionString(IUmbracoBuilder builder) {
        builder.AddAzureBlobMediaFileSystem();
        builder.AddAzureBlobImageSharpCache();

        var azureBlobSection = builder.Config
                                      .GetSection(AzureBlobStorage.MediaSectionKey)
                                      .Get<AzureBlobFileSystemOptions>();

        builder.Services.AddDataProtection().PersistKeysToAzureBlobStorage(azureBlobSection.ConnectionString,
                                                                           azureBlobSection.ContainerName,
                                                                           DataKeysXml);
    }
}
