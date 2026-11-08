using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Security;
using Perplex.ContentBlocks.Rendering;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexBlockViewModelFactory<TBlock, TViewModel> : ContentBlockViewModelFactory<TBlock>
    where TBlock : class, IPublishedElement
    where TViewModel : IPerplexBlockViewModel<TBlock> {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;
    private readonly IRenderFailures _renderFailures;
    private readonly Func<IServiceProvider, PerplexBlockParameters<TBlock>, TViewModel> _constructViewModel;

    public PerplexBlockViewModelFactory(IServiceProvider serviceProvider,
                                        Func<IServiceProvider, PerplexBlockParameters<TBlock>, TViewModel> constructViewModel) {
        _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        _logger = serviceProvider.GetRequiredService<ILogger<PerplexBlockViewModelFactory<TBlock, TViewModel>>>();
        _renderFailures = serviceProvider.GetRequiredService<IRenderFailures>();
        _constructViewModel = constructViewModel;
    }

    public override IContentBlockViewModel<TBlock> Create(TBlock content,
                                                          Guid id,
                                                          Guid definitionId,
                                                          Guid layoutId) {
        try {
            return CreateViewModel(content, id, definitionId, layoutId);
        } catch (Exception ex) when (_renderFailures.CanIsolate(ex)) {
            _logger.LogError(ex,
                             "Failed to create the view model of block {ContentTypeAlias} {BlockId}",
                             content.ContentType.Alias,
                             id);

            return new FailedPerplexBlockViewModel<TBlock>(content, id, definitionId, layoutId, ex);
        }
    }

    private TViewModel CreateViewModel(TBlock content, Guid id, Guid definitionId, Guid layoutId) {
        var serviceProvider = _httpContextAccessor.HttpContext.RequestServices;
        
        var stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();
        var blockPipeline = serviceProvider.GetRequiredService<IBlockPipeline>();
        var modulesData = blockPipeline.RunAsync(content).GetAwaiter().GetResult();
        
        var blockParameters = new PerplexBlockParameters<TBlock>(s => stringLocalizer.Get(Constants.TextFolders.Blocks,
                                                                                          content.ContentType.Alias.Pascalize(),
                                                                                          s),
                                                                 content,
                                                                 modulesData,
                                                                 id,
                                                                 definitionId,
                                                                 layoutId);
        
        return _constructViewModel(serviceProvider, blockParameters);
    }
}

public static class PerplexBlockViewModelFactory {
    public static IContentBlockViewModelFactory Default(IServiceProvider serviceProvider, Type blockType) {
        var factory = typeof(PerplexBlockViewModelFactory).CallStaticMethod(nameof(Default))
                                                          .OfGenericType(blockType)
                                                          .WithParameter(typeof(IServiceProvider), serviceProvider)
                                                          .Run();

        return (IContentBlockViewModelFactory) factory;
    }

    public static IContentBlockViewModelFactory<TBlock> Default<TBlock>(IServiceProvider serviceProvider)
        where TBlock : class, IPublishedElement {
        return new PerplexBlockViewModelFactory<TBlock, PerplexBlockViewModel<TBlock>>(serviceProvider,
                                                                                       (_, p) => new PerplexBlockViewModel<TBlock>(p));
    }
}
