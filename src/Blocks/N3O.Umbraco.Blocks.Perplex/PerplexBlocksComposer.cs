using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Blocks.Perplex.Extensions;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Utilities;
using Perplex.ContentBlocks.Categories;
using Perplex.ContentBlocks.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Blocks.Perplex;

[ComposeAfter(typeof(BlocksComposer))]
public class PerplexBlocksComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        BlocksComponent.LoadDefinitions(builder, WebHostEnvironment);

        builder.Services.AddTransient<IBlocksRenderer, PerplexBlocksRenderer>();
        builder.Services.AddTransient<IPerplexBlockTypesService, PerplexBlockTypesService>();

        foreach (var blockDefinition in BlocksComponent.BlockDefinitions) {
            RegisterDefaultViewModel(builder, blockDefinition.Alias);
        }
    
        builder.Components().Append<BlocksComponent>();
    }

    private void RegisterDefaultViewModel(IUmbracoBuilder builder, string contentTypeAlias) {
        var blockType = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                    t.IsSubclassOfType(typeof(PublishedElementModel)))
                                     .SingleOrDefault(t => AliasHelper.ContentTypeAlias(t).EqualsInvariant(contentTypeAlias));

        if (blockType != null) {
            builder.AddDefaultBlockViewModel(blockType);
        }
    }
}

public class BlocksComponent : IComponent {
    public static IReadOnlyList<PerplexBlockDefinition> BlockDefinitions { get; private set; }

    private readonly IRuntimeState _runtimeState;
    private readonly Lazy<IPerplexBlockTypesService> _blockTypesService;
    private readonly Lazy<ILookups> _lookups;
    private readonly Lazy<IContentBlockDefinitionRepository> _blockDefinitionsRepository;
    private readonly Lazy<IContentBlockCategoryRepository> _blockCategoriesRepository;

    public BlocksComponent(IRuntimeState runtimeState,
                           Lazy<IPerplexBlockTypesService> blockTypesService,
                           Lazy<ILookups> lookups,
                           Lazy<IContentBlockDefinitionRepository> blockDefinitionsRepository,
                           Lazy<IContentBlockCategoryRepository> blockCategoriesRepository) {
        _runtimeState = runtimeState;
        _blockTypesService = blockTypesService;
        _lookups = lookups;
        _blockDefinitionsRepository = blockDefinitionsRepository;
        _blockCategoriesRepository = blockCategoriesRepository;
    }

    public void Initialize() {
        if (_runtimeState.Level == RuntimeLevel.Run) {
            var blockCategories = _lookups.Value.GetAll<PerplexBlockCategory>().OrderBy(x => x.Order).ToList();

            _blockCategoriesRepository.Value.Remove(global::Perplex.ContentBlocks.Constants.Categories.Content);
            _blockCategoriesRepository.Value.Remove(global::Perplex.ContentBlocks.Constants.Categories.Headers);
            
            blockCategories.Do(x => _blockCategoriesRepository.Value.Add(x));

            BlockDefinitions.Do(x => {
                _blockTypesService.Value.CreateTypes(x);
                _blockDefinitionsRepository.Value.Add(x);
            });
        }
    }

    public void Terminate() { }
    
    public static void LoadDefinitions(IUmbracoBuilder builder, IWebHostEnvironment webHostEnvironment) {
        BlockDefinitions = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                       t.ImplementsInterface<IPerplexBlockBuilder>() &&
                                                       t.HasParameterlessConstructor())
                                        .Select(t => (IPerplexBlockBuilder) Activator.CreateInstance(t))
                                        .SelectMany(x => x.Build(builder, webHostEnvironment))
                                        .ToList();
    }
}
