using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Parameters;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Search.Typesense.Commands;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Typesense;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Search.Typesense.Notifications;

public class TypesenseStartupTasks : INotificationAsyncHandler<UmbracoApplicationStartedNotification> {
    private readonly ILogger _logger;
    private readonly ITypesenseClient _typesenseClient;
    private readonly ITypesenseJsonProvider _typesenseJsonProvider;
    private readonly IBackgroundJob _backgroundJob;

    public TypesenseStartupTasks(ILogger<TypesenseStartupTasks> logger,
                                 ITypesenseClient typesenseClient,
                                 ITypesenseJsonProvider typesenseJsonProvider,
                                 IBackgroundJob backgroundJob) {
        _logger = logger;
        _typesenseClient = typesenseClient;
        _typesenseJsonProvider = typesenseJsonProvider;
        _backgroundJob = backgroundJob;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification,
                                  CancellationToken cancellationToken) {
        if (_typesenseClient.HasValue()) {
            foreach (var collection in TypesenseHelper.GetAllCollections()) {
                try {
                    await MigrateCollectionAsync(collection);
                } catch (Exception ex) {
                    _logger.LogError(ex,
                                     "Failed to migrate Typesense collection {Collection}",
                                     collection.Name.Resolve());
                }
            }
        }
    }

    private async Task MigrateCollectionAsync(CollectionInfo collectionInfo) {
        var collection = await TryGetCollectionAsync(collectionInfo.Name.Resolve());

        collection = await TryDropCollectionIfOldVersionAsync(collection, collectionInfo);
        
        if (collection == null) {
            await CreateCollectionAsync(collectionInfo);
            
            EnqueueIndexing(collectionInfo);
        }
    }

    private async Task<CollectionResponse> TryGetCollectionAsync(string collectionName) {
        try {
            return await _typesenseClient.RetrieveCollection(collectionName);
        } catch (TypesenseApiNotFoundException) {
            return null;
        }
    }
    
    private async Task<CollectionResponse> TryDropCollectionIfOldVersionAsync(CollectionResponse collection,
                                                                              CollectionInfo collectionInfo) {
        if (collection != null) {
            var metadataVersion = GetVersionFromMetadata(collection);

            if (metadataVersion != collectionInfo.Version) {
                await _typesenseClient.DeleteCollection(collectionInfo.Name.Resolve());

                return null;
            }
        }

        return collection;
    }

    private async Task CreateCollectionAsync(CollectionInfo collectionInfo) {
        var collectionName = collectionInfo.Name.Resolve();
        
        var schema = new Schema(collectionName, collectionInfo.Fields) {
            EnableNestedFields =  true,
            Metadata = new Dictionary<string, object> {
                { TypesenseConstants.MetadataKeys.Version, collectionInfo.Version }
            }
        };

        await _typesenseClient.CreateCollection(schema);
    }

    private void EnqueueIndexing(CollectionInfo collection) {
        foreach (var contentType in collection.ContentTypeAliases.OrEmpty()) {
            _backgroundJob.EnqueueCommand<IndexContentsOfTypeCommand>(m => m.Add<ContentType>(contentType), contentType);
        }
    }
    
    private int? GetVersionFromMetadata(CollectionResponse collection) {
        var raw = collection?.Metadata?.TryGet(TypesenseConstants.MetadataKeys.Version);

        if (raw == null) {
            return null;
        } else if (raw is JsonElement element) {
            return _typesenseJsonProvider.DeserializeObject<int?>(element.GetRawText());
        } else if (raw is int version) {
            return version;
        } else {
            throw new Exception($"Version metadata {raw.ToString().Quote()} has an unrecognised format");
        }
    }
}