using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using Perplex.ContentBlocks.Rendering;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexBlockViewModelFactory<TBlock, TViewModel> : ContentBlockViewModelFactory<TBlock>
    where TBlock : class, IPublishedElement
    where TViewModel : IPerplexBlockViewModel<TBlock> {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Func<IServiceProvider, PerplexBlockParameters<TBlock>, TViewModel> _constructViewModel;

    public PerplexBlockViewModelFactory(IHttpContextAccessor httpContextAccessor,
                                        Func<IServiceProvider, PerplexBlockParameters<TBlock>, TViewModel> constructViewModel) {
        _httpContextAccessor = httpContextAccessor;
        _constructViewModel = constructViewModel;
    }

    public override IContentBlockViewModel<TBlock> Create(TBlock content,
                                                          Guid id,
                                                          Guid definitionId,
                                                          Guid layoutId) {
        try {
            return CreateViewModel(content, id, definitionId, layoutId);
        } catch (Exception ex) {
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
    public static IContentBlockViewModelFactory Default(IHttpContextAccessor httpContextAccessor, Type blockType) {
        return (IContentBlockViewModelFactory) typeof(PerplexBlockViewModelFactory).CallStaticMethod(nameof(Default))
                                                                                   .OfGenericType(blockType)
                                                                                   .WithParameter(typeof(IHttpContextAccessor), httpContextAccessor)
                                                                                   .Run();
    }

    public static PerplexBlockViewModelFactory<TBlock, PerplexBlockViewModel<TBlock>> Default<TBlock>(IHttpContextAccessor httpContextAccessor)
        where TBlock : class, IPublishedElement {
        return new PerplexBlockViewModelFactory<TBlock, PerplexBlockViewModel<TBlock>>(httpContextAccessor,
                                                                                       (_, p) => new PerplexBlockViewModel<TBlock>(p));
    }
}
