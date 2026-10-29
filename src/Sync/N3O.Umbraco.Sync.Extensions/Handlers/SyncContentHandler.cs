using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using uSync.BackOffice;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Sync;
using uSync.Expansions.Core;
using uSync.Publisher.Client;
using uSync.Publisher.Models;
using uSync.Publisher.Publishers;
using uSync.Publisher.Services;

namespace N3O.Umbraco.Sync.Extensions.Handlers;

public class SyncContentHandler : IRequestHandler<SyncContentCommand, SyncContentReq, None> {
    private static readonly string Document = global::Umbraco.Cms.Core.Constants.UdiEntityType.Document;
    
    private readonly IContentLocator _contentLocator;
    private readonly ISyncPublisherActionService _syncPublisherActionService;
    private readonly SyncPublisherFactory _syncPublisherFactory;

    public SyncContentHandler(IContentLocator contentLocator,
                              ISyncPublisherActionService syncPublisherActionService,
                              SyncPublisherFactory syncPublisherFactory) {
        _contentLocator = contentLocator;
        _syncPublisherActionService = syncPublisherActionService;
        _syncPublisherFactory = syncPublisherFactory;
    }

    public async Task<None> Handle(SyncContentCommand req, CancellationToken cancellationToken) {
        var content = _contentLocator.ById(req.Model.ContentId.GetValueOrThrow());
        var publisher = _syncPublisherFactory.GetPublisher(req.Model.ServerAlias);

        if (publisher is not SyncRealtimePublisher) {
            throw new Exception($"Sync of {req.Model.ContentId} needs server {req.Model.ServerAlias} to use the " +
                                $"realtime publisher, not {publisher.Alias}");
        }

        var syncItem = new SyncItem();
        syncItem.Change = ChangeType.Create;
        syncItem.Flags = DependencyFlags.PublishedDependencies;
        syncItem.Udi = Udi.Create(Document, req.Model.ContentId.GetValueOrThrow());
        syncItem.Name = content.Name;

        var options = new SyncPackOptions();
        options.PrimaryType = syncItem.Udi.EntityType;
        options.SkipReport = true;

        var process = new SyncActionProcess();
        process.Id = Guid.NewGuid();
        process.ActionAlias = string.Empty;
        process.Server = req.Model.ServerAlias;
        process.Mode = PublishMode.Push;
        process.Items = [syncItem];
        process.Steps = new SyncActionStepInfo();
        process.Options = options;

        var failedItems = new List<uSyncAction>();
        var result = default(PublisherActionResult);

        do {
            cancellationToken.ThrowIfCancellationRequested();

            var action = await _syncPublisherActionService.GetAction(process.Server,
                                                                     process.ActionAlias,
                                                                     process.Mode,
                                                                     process.Options,
                                                                     process.Items);

            result = await _syncPublisherActionService.PerformAction(MakePublishRequest(process, action), null);

            if (!result.Success) {
                throw new Exception($"Sync of {req.Model.ContentId} failed with error: {result.Error.Message}");
            }

            failedItems.AddRange(result.Actions.OrEmpty().Where(IsError));

            UpdateProcess(process, result);
        } while (!result.ProcessComplete);

        if (failedItems.Any()) {
            var failures = failedItems.Select(x => $"{x.Name} ({x.Change}: {x.Message})").Distinct().ToCsv(true);

            throw new Exception($"Sync of {req.Model.ContentId} failed to import {failures}");
        }
        
        return None.Empty;
    }

    private bool IsError(uSyncAction itemResult) {
        return uSync.BackOffice.uSyncActionExtensions.ContainsErrors([itemResult]);
    }

    private PublisherActionRequest MakePublishRequest(SyncActionProcess process, PublisherAction action) {
        var request = new PublisherActionRequest();
        request.Id = process.Id;
        request.Server = process.Server;
        request.Mode = process.Mode;
        request.Items = process.Items;
        request.ActionAlias = action.Alias;
        request.StepIndex = process.Steps.StepIndex;
        request.HandlerFolder = process.Steps.HandlerFolder;
        request.PageNumber = process.Steps.PageNumber;
        request.Options = process.Options;
        request.AdditionalData = process.AdditionalData;

        return request;
    }

    private void UpdateProcess(SyncActionProcess process, PublisherActionResult result) {
        process.Id = result.Id;
        process.ActionAlias = result.NextAction;
        process.Items = result.Items;
        process.Steps.StepIndex = result.StepIndex;
        process.Steps.PageNumber = result.NextPage;
        process.Steps.HandlerFolder = result.NextFolder;
        process.AdditionalData = result.AdditionalData;
    }
}