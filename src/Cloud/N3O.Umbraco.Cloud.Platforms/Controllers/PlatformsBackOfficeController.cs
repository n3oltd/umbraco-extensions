using Flurl;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Utilities;
using Slugify;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

[ApiDocument(PlatformsConstants.BackOfficeApiName)]
public class PlatformsBackOfficeController : BackofficeAuthorizedApiController {
    private readonly IContentCache _contentCache;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ISlugHelper _slugHelper;

    public PlatformsBackOfficeController(IContentCache contentCache,
                                         IContentService contentService,
                                         IContentTypeService contentTypeService,
                                         ISlugHelper slugHelper) {
        _contentCache = contentCache;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _slugHelper = slugHelper;
    }

    [HttpGet("contentUrls/{contentId:guid}")]
    public async Task<ActionResult<ContentUrlsRes>> GetContentUrls([FromRoute] Guid contentId) {
        var content = _contentService.GetById(contentId);
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        var isCampaign = content != null && content.IsCampaign(_contentTypeService);
        var isCrowdfunder = content != null && content.IsCrowdfunder();
        var isOffering = content != null && content.IsOffering(_contentTypeService);

        if (content == null ||
            !content.Published ||
            urlSettings == null ||
            (!isCampaign && !isCrowdfunder && !isOffering)) {
            return null;
        }

        string path;

        if (isCampaign) {
            path = _contentCache.GetCampaignPath(_slugHelper, content.Name);
        } else if (isCrowdfunder) {
            path = _contentCache.GetCrowdfundingCampaignPath(_slugHelper, content.Name);
        } else {
            var parent = _contentService.GetById(content.ParentId);

            path = parent == null ? null : _contentCache.GetOfferingPath(_slugHelper, parent.Name, content.Name);
        }

        var res = new ContentUrlsRes();

        if (path.HasValue()) {
            if (urlSettings.StagingBaseUrl.HasValue()) {
                res.StagingUrl = new Url(urlSettings.StagingBaseUrl).AppendPathSegment(path);
            }

            if (urlSettings.ProductionBaseUrl.HasValue()) {
                res.ProductionUrl = new Url(urlSettings.ProductionBaseUrl).AppendPathSegment(path);
            }
        }

        return Ok(res);
    }
}