using Flurl;
using Microsoft.AspNetCore.Hosting;
using N3O.Umbraco.Content;

namespace N3O.Umbraco.Utilities;

public class UrlBuilder : IUrlBuilder {
    private readonly IContentCache _contentCache;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UrlBuilder(IContentCache contentCache, IWebHostEnvironment webHostEnvironment) {
        _contentCache = contentCache;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public Url Root() {
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        if (urlSettings == null) {
            return null;
        }

        return urlSettings.BaseUrl(_webHostEnvironment);
    }
}
