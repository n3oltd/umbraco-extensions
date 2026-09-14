using Microsoft.AspNetCore.Hosting;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Features;
using N3O.Umbraco.Hosting;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Llms;

public class LlmsTxt : ILlmsTxt {
    private const string LlmsFileName = "llms.txt";

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IContentCache _contentCache;

    public LlmsTxt(IUmbracoContextFactory umbracoContextFactory,
                   IWebHostEnvironment webHostEnvironment,
                   IContentCache contentCache) {
        _umbracoContextFactory = umbracoContextFactory;
        _webHostEnvironment = webHostEnvironment;
        _contentCache = contentCache;
    }

    public async Task PublishAsync() {
        if (FeatureFlags.IsNotSet(FeatureFlags.LlmsTxt)) {
            return;
        }

        var llmsTxt = GetContent();

        if (llmsTxt.HasValue()) {
            await WebRoot.SaveTextAsync(_webHostEnvironment, LlmsFileName, llmsTxt);
        } else {
            Remove();
        }
    }

    public void Remove() {
        if (FeatureFlags.IsNotSet(FeatureFlags.LlmsTxt)) {
            return;
        }

        WebRoot.DeleteFile(_webHostEnvironment, LlmsFileName);
    }

    private string GetContent() {
        using (_umbracoContextFactory.EnsureUmbracoContext()) {
            var llmsSettings = _contentCache.Single<LlmsSettingsContent>();

            return llmsSettings?.Markdown;
        }
    }
}
