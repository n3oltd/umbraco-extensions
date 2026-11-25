using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Blocks.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks;

public class UmbracoBlocksRenderer : BlocksRenderer<IEnumerable<IBlockReference<IPublishedElement, IPublishedElement>>> {
    private readonly ModelsBuilderSettings _modelBuilderSettings;
    private readonly IServiceProvider _serviceProvider;

    public UmbracoBlocksRenderer(IEnumerable<IBlocksRendererPostProcessor> postProcessors,
                                 IOptions<ModelsBuilderSettings> modelBuilderSettings,
                                 IServiceProvider serviceProvider)
        : base(postProcessors) {
        _modelBuilderSettings = modelBuilderSettings.Value;
        _serviceProvider = serviceProvider;
    }
    
    public override async Task<HtmlString> RenderBlocksAsync(IPublishedContent content, string propertyName) {
        var property = GetPropertyAs(content, propertyName);
        
        var viewModels = GetViewModels(property).ToList();
        var viewPaths = GetViewPaths(property);

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

    private IEnumerable<IBlockViewModel> GetViewModels(IEnumerable<IBlockReference<IPublishedElement, IPublishedElement>> blockItems) {
        foreach (var blockItem in blockItems) {
            var blockModelsBuilderType = ModelsHelper.GetOrCreateModelsBuilderType(_modelBuilderSettings.ModelsNamespace,
                                                                                   blockItem.Content.ContentType.Alias);

            var settingsModelsBuilderType = blockItem.Settings.HasValue()
                                                ? ModelsHelper.GetOrCreateModelsBuilderType(_modelBuilderSettings.ModelsNamespace,
                                                                                            blockItem.Settings.ContentType.Alias)
                                                : typeof(None);

            var factoryType = typeof(IBlockViewModelFactory<,>).MakeGenericType(blockModelsBuilderType,
                                                                                settingsModelsBuilderType);

            var factory = (IBlockViewModelFactory) _serviceProvider.GetService(factoryType);

            if (factory == null) {
                factory = BlockViewModelFactory.Default(() => _serviceProvider,
                                                        blockModelsBuilderType,
                                                        settingsModelsBuilderType);
            }

            var viewModel = blockItem.Settings.HasValue()
                                ? factory.Create(blockItem.Content, blockItem.Settings)
                                : factory.Create(blockItem.Content, None.Empty);

            yield return viewModel;
        }
    }

    private IEnumerable<string> GetViewPaths(IEnumerable<IBlockReference<IPublishedElement, IPublishedElement>> blockItems) {
        foreach (var blockItem in blockItems) {
            yield return $"{blockItem.Content.ContentType.Alias.ToBlockViewsPath()}.cshtml";
        }
    }
}