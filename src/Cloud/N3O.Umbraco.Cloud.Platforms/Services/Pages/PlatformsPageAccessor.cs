using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.ContentFinders;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsPageAccessor : IPlatformsPageAccessor {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContentCache _contentCache;
    private readonly ICultureAccessor _cultureAccessor;
    private readonly ICdnClient _cdnClient;
    private readonly IJsonProvider _jsonProvider;

    public PlatformsPageAccessor(IHttpContextAccessor httpContextAccessor,
                                 IContentCache contentCache,
                                 ICultureAccessor cultureAccessor,
                                 ICdnClient cdnClient,
                                 IJsonProvider jsonProvider) {
        _httpContextAccessor = httpContextAccessor;
        _contentCache = contentCache;
        _cultureAccessor = cultureAccessor;
        _cdnClient = cdnClient;
        _jsonProvider = jsonProvider;
    }
    
    public async Task<GetPageResult> GetAsync(CancellationToken cancellationToken = default) {
        if (_httpContextAccessor.HttpContext == null) {
            return null;
        }

        var key = nameof(PlatformsPageAccessor);

        if (!_httpContextAccessor.HttpContext.Items.ContainsKey(key)) {
            var requestUri = _httpContextAccessor.HttpContext.Request.Uri();

            var getPageResult = await GetAsync(requestUri, cancellationToken);

            // Platforms pages change without an Umbraco publish, so no purge would reach an edge-cached copy.
            if (getPageResult.HasValue()) {
                EdgeCaching.Prevent(_httpContextAccessor.HttpContext);
            }

            _httpContextAccessor.HttpContext.Items[key] = getPageResult;
        }
        
        return (GetPageResult) _httpContextAccessor.HttpContext.Items[key];
    }
    
    private async Task<GetPageResult> GetAsync(Uri requestUri, CancellationToken cancellationToken) {
        var culture = _cultureAccessor.GetCulture();
        var fallbacks = new List<SpecialContent>();

        foreach (var platformsPageRoute in PlatformsPageRoute.All) {
            var platformsPath = SpecialContentPathParser.ParseUri(_contentCache,
                                                                  platformsPageRoute.Parent,
                                                                  requestUri,
                                                                  culture);

            if (!platformsPath.HasValue()) {
                continue;
            }
        
            var currentPath = platformsPath;

            do {
                var getPageResult = await _cdnClient.DownloadPlatformsPageAsync(_jsonProvider,
                                                                                platformsPageRoute.ContentKind,
                                                                                platformsPageRoute.Parent,
                                                                                currentPath,
                                                                                cancellationToken);

                if (getPageResult.HasValue() && getPageResult.IsError) {
                    return getPageResult;
                }

                if (getPageResult.HasValue()) {
                    if (getPageResult.IsRedirect) {
                        return getPageResult;
                    } else {
                        if (currentPath != platformsPath) {
                            return GetPageResult.ForRedirect(getPageResult.Page.Url.AbsolutePath, true);
                        } else {
                            return getPageResult;
                        }
                    }
                }
            
                var lastPathSegment = currentPath.StripTrailingSlash().LastIndexOf('/');
            
                if (lastPathSegment > 0) {
                    currentPath = currentPath.Substring(0, lastPathSegment);
                } else {
                    break;
                }
            } while (currentPath.HasValue());
            
            fallbacks.Add(platformsPageRoute.Parent);
        }
        
        if (fallbacks.Any()) {
            return GetPageResult.ForRedirect(SpecialContentPathParser.GetPath(_contentCache,
                                                                              fallbacks.First(),
                                                                              culture),
                                             true);
        } else {
            return null;
        }
    }
}
