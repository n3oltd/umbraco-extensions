using Jumoo.Json;
using Jumoo.Processing.Core.Pipelines;
using Jumoo.Processing.Core.Pipelines.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using uSync.BackOffice.Models;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Sync;
using uSync.Publisher.Models;
using uSync.Publisher.Process.Models;
using uSync.Publisher.Publishers;
using uSync.Publisher.Strategies.Models;

namespace N3O.Umbraco.Sync.Extensions.Handlers;

public class SyncContentHandler : IRequestHandler<SyncContentCommand, SyncContentReq, None> {
    private const string ActionsResultKey = "actions";
    private static readonly string Document = global::Umbraco.Cms.Core.Constants.UdiEntityType.Document;

    private readonly IContentLocator _contentLocator;
    private readonly IPipelineService _pipelineService;
    private readonly SyncPublisherFactory _syncPublisherFactory;
    private readonly IUserService _userService;

    public SyncContentHandler(IContentLocator contentLocator,
                              IPipelineService pipelineService,
                              SyncPublisherFactory syncPublisherFactory,
                              IUserService userService) {
        _contentLocator = contentLocator;
        _pipelineService = pipelineService;
        _syncPublisherFactory = syncPublisherFactory;
        _userService = userService;
    }

    public async Task<None> Handle(SyncContentCommand req, CancellationToken cancellationToken) {
        var requestId = req.Model.RequestId.GetValueOrThrow();
        var contentId = req.Model.ContentId.GetValueOrThrow();
        var content = _contentLocator.ById(req.Model.ContentId.GetValueOrThrow());

        var syncItem = new SyncItem {
            Udi = Udi.Create(Document, contentId),
            Name = content.Name
        };
        syncItem.Change = ChangeType.Create;
        syncItem.Flags = DependencyFlags.PublishedDependencies;

        var options = new PublisherProcessingOptions();
        options.Mode = PublishMode.Push;
        options.Server = req.Model.ServerAlias;
        options.EntityType = Document;
        options.Items = [syncItem];
        options.PublisherOptions = new SyncPublisherOptions();
        options.PublisherOptions.PublishedDependencies = true;

        // Pipelines require a user with uSync's Push permission and a background job has none
        var user = await _userService.GetAsync(global::Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        var publisher = _syncPublisherFactory.GetPublisherByServer(req.Model.ServerAlias);

        if (publisher is not SyncRealtimePublisher) {
            throw new Exception($"Sync of {contentId} needs server {req.Model.ServerAlias} to use the realtime publisher, not {publisher.Alias}");
        }

        var createPipelineOptions = new CreatePipelineOptions();
        createPipelineOptions.Alias = publisher.Processor;
        createPipelineOptions.Strategy = publisher.GetStrategy(PublishMode.Push);
        createPipelineOptions.User = user;

        var pipeline = await _pipelineService.CreatePipeline(createPipelineOptions);

        try {
            await _pipelineService.UpdateOptions(pipeline.Id, options, user);

            do {
                cancellationToken.ThrowIfCancellationRequested();

                pipeline = await _pipelineService.Process(pipeline.Id, user, requestId.ToString(), false);
            } while (pipeline.State.Status is PipelineStatus.Running or PipelineStatus.Waiting);
        } catch {
            await _pipelineService.ClearPipeline(pipeline.Id, user);

            throw;
        }

        if (pipeline.State.Status is PipelineStatus.Completed or PipelineStatus.Failed) {
            await _pipelineService.ClearPipeline(pipeline.Id, user);
        }

        if (pipeline.State.Status == PipelineStatus.Failed) {
            throw new Exception($"Sync of {contentId} failed with error: {pipeline.Results.Error.Message}");
        }

        var failedItems = GetItemResults(pipeline).Where(x => !x.Success).ToList();

        if (failedItems.Any()) {
            throw new Exception($"Sync of {contentId} failed to import {failedItems.Select(x => $"{x.Name} ({x.Message})").ToCsv(true)}");
        }

        return None.Empty;
    }

    private IReadOnlyList<uSyncActionView> GetItemResults(IPipeline pipeline) {
        var actions = default(object);

        if (pipeline.Results?.Results.TryGetValue(ActionsResultKey, out actions) != true || actions == null) {
            return [];
        } else if (actions is IEnumerable<uSyncActionView> actionViews) {
            return actionViews.ToList();
        } else if (actions is JsonNode jsonNode) {
            return jsonNode.Deserialize<List<uSyncActionView>>(JsonTextOptions.GetOptions());
        } else {
            throw new Exception($"Pipeline {pipeline.Id} has {ActionsResultKey} of unrecognised type {actions.GetType()}");
        }
    }
}
