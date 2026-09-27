using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Pages;
using N3O.Umbraco.Templates.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Templates.Modules;

public class MergeModelsPageModule : IPageModule {
    private readonly ConcurrentDictionary<IPublishedContent, IReadOnlyDictionary<string, object>> _mergeModelsCache = new();
    private readonly IEnumerable<IMergeModelsProvider> _mergeModelsProviders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MergeModelsPageModule> _logger;

    public MergeModelsPageModule(IEnumerable<IMergeModelsProvider> mergeModelProviders,
                                 IHttpContextAccessor httpContextAccessor,
                                 ILogger<MergeModelsPageModule> logger) {
        _mergeModelsProviders = mergeModelProviders;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public bool ShouldExecute(IPublishedContent page) => true;

    public async Task<object> ExecuteAsync(IPublishedContent page, CancellationToken cancellationToken) {
        var mergeModels = await _mergeModelsProviders.GetMergeModelsAsync(_logger,
                                                                          _httpContextAccessor.HttpContext,
                                                                          page,
                                                                          _mergeModelsCache,
                                                                          cancellationToken);
        
        return mergeModels;
    }

    public string Key => TemplateConstants.PageModuleKeys.MergeModels;
}
