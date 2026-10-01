using Jumoo.Processing.Core.Pipelines;
using Jumoo.Processing.Core.Pipelines.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Sync;
using uSync.Publisher.Models;
using uSync.Publisher.Process.Models;
using uSync.Publisher.Publishers;
using uSync.Publisher.Strategies.Models;

namespace N3O.Umbraco.Sync.Extensions.Handlers;

public class SyncContentHandler : IRequestHandler<SyncContentCommand, SyncContentReq, None> {
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

        // uSync.Publisher refuses to create or process a pipeline without a back-office user holding
        // uSync.UserPermission.Push, and a background job has no current user. uSync grants that permission to
        // the admin group, which the super user belongs to.
        var user = await _userService.GetAsync(global::Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        var publisher = _syncPublisherFactory.GetPublisherByServer(req.Model.ServerAlias);

        var createPipelineOptions = new CreatePipelineOptions();
        createPipelineOptions.Alias = publisher.Processor;
        createPipelineOptions.Strategy = publisher.GetStrategy(PublishMode.Push);
        createPipelineOptions.User = user;

        var pipeline = await _pipelineService.CreatePipeline(createPipelineOptions);

        await _pipelineService.UpdateOptions(pipeline.Id, options, user);

        // Each call runs one pipeline step. Waiting marks a step that would show a back-office screen, which
        // processing moves past; Background means uSync's own queue finishes the push.
        do {
            cancellationToken.ThrowIfCancellationRequested();

            pipeline = await _pipelineService.Process(pipeline.Id, user, requestId.ToString(), false);
        } while (pipeline.State.Status is PipelineStatus.Running or PipelineStatus.Waiting);

        if (pipeline.State.Status is PipelineStatus.Completed or PipelineStatus.Failed) {
            await _pipelineService.ClearPipeline(pipeline.Id, user);
        }

        if (pipeline.State.Status == PipelineStatus.Failed) {
            throw new Exception($"Sync of {contentId} failed with error: {pipeline.Results.Error.Message}");
        }

        return None.Empty;
    }
}
