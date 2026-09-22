using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using Perplex.ContentBlocks.Definitions;
using Perplex.ContentBlocks.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexBlocksRenderer : BlocksRenderer<ContentBlocks> {
    private readonly IContentBlockDefinitionRepository _blockDefinitions;
    private readonly ModelsBuilderSettings _modelBuilderSettings;
    private readonly IServiceProvider _serviceProvider;

    public PerplexBlocksRenderer(IEnumerable<IBlocksRendererPostProcessor> postProcessors,
                                 IContentBlockDefinitionRepository blockDefinitions,
                                 IOptions<ModelsBuilderSettings> modelBuilderSettings,
                                 IServiceProvider serviceProvider)
        : base(postProcessors) {
        _blockDefinitions = blockDefinitions;
        _modelBuilderSettings = modelBuilderSettings.Value;
        _serviceProvider = serviceProvider;
    }
    
    public override async Task<HtmlString> RenderBlocksAsync(IPublishedContent content, string propertyName) {
        var property = GetPropertyAs(content, propertyName);
        
        var viewModels = GetViewModels(property).ToList();
        var viewPaths = GetViewPaths(property).ToList();

        if (viewModels.Count() != viewPaths.Count()) {
            throw new Exception($"{nameof(GetViewModels)} and {nameof(GetViewPaths)} returned different numbers of items");
        }

        var html = new StringBuilder();

        foreach (var (viewModel, index) in viewModels.SelectWithIndex()) {
            var viewPath = viewPaths.ElementAt(index);
            
            var blockHtml = await RenderViewAsync(content, viewPath, viewModel);
            
            html.Append(blockHtml);
        } 

        return new HtmlString(html.ToString());
    }

    private IEnumerable<IBlockViewModel> GetViewModels(ContentBlocks contentBlocks) {
        foreach (var contentBlockModel in contentBlocks.Blocks) {
            var blockModelsBuilderType = ModelsHelper.GetOrCreateModelsBuilderType(_modelBuilderSettings.ModelsNamespace,
                                                                                   contentBlockModel.Content.ContentType.Alias);

            var factoryType = typeof(IContentBlockViewModelFactory<>).MakeGenericType(blockModelsBuilderType);
            
            var factory = (IContentBlockViewModelFactory) _serviceProvider.GetService(factoryType);
        
            var blockModel = (IPerplexBlockViewModel) factory.Create(contentBlockModel.Content,
                                                                     contentBlockModel.Id,
                                                                     contentBlockModel.DefinitionId,
                                                                     contentBlockModel.LayoutId);

            yield return blockModel;
        }
    }

    private IEnumerable<string> GetViewPaths(ContentBlocks contentBlocks) {
        foreach (var contentBlockModel in contentBlocks.Blocks) {
            var definition = _blockDefinitions.GetById(contentBlockModel.DefinitionId);

            if (definition == null) {
                throw new Exception($"No block definition found with id {contentBlockModel.DefinitionId}");
            }

            var layout = definition.Layouts.FirstOrDefault(x => x.Id == contentBlockModel.LayoutId);

            if (layout == null) {
                throw new Exception($"Block definition {definition.Name.Quote()} has no layout with id " +
                                    $"{contentBlockModel.LayoutId}");
            }

            yield return layout.ViewPath;
        }
    }
}