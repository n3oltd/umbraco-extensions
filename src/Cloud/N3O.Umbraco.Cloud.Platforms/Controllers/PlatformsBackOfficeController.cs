using Flurl;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Authentication.Auth0;
using N3O.Umbraco.Authentication.Auth0.Lookups;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Utilities;
using Slugify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

[ApiDocument(PlatformsConstants.BackOfficeApiName)]
public class PlatformsBackOfficeController : BackofficeAuthorizedApiController {
    private const string AnalyticsFieldsOptionsSource = "tags.analyticsFields";

    private readonly Auth0TokenAccessor _auth0TokenAccessor;
    private readonly Lazy<ClientFactory<TagsOptionsSourcesClient>> _clientFactory;
    private readonly IContentCache _contentCache;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILookups _lookups;
    private readonly ISlugHelper _slugHelper;

    public PlatformsBackOfficeController(Auth0TokenAccessor auth0TokenAccessor,
                                         Lazy<ClientFactory<TagsOptionsSourcesClient>> clientFactory,
                                         IContentCache contentCache,
                                         IContentService contentService,
                                         IContentTypeService contentTypeService,
                                         ILookups lookups,
                                         ISlugHelper slugHelper) {
        _auth0TokenAccessor = auth0TokenAccessor;
        _clientFactory = clientFactory;
        _contentCache = contentCache;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _lookups = lookups;
        _slugHelper = slugHelper;
    }

    [HttpGet("contentUrls/{contentId:guid}")]
    public async Task<ActionResult<ContentUrlsRes>> GetContentUrls([FromRoute] Guid contentId) {
        var content = _contentService.GetById(contentId);
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        var isCampaign = content != null && content.IsCampaign(_contentTypeService);
        var isCrowdfundingCampaign = content != null && content.IsCrowdfundingCampaign();

        if (content == null ||
            !content.Published ||
            urlSettings == null ||
            !content.HasPlatformsUrl(_contentTypeService)) {
            return null;
        }

        string path;

        if (isCampaign) {
            path = _contentCache.GetCampaignPath(_slugHelper, content.Name);
        } else if (isCrowdfundingCampaign) {
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

    [HttpGet("tags/keys")]
    public async Task<ActionResult<IEnumerable<TagKeyRes>>> GetTagKeys(CancellationToken cancellationToken) {
        var analyticsFields = await _lookups.GetAllAsync<AnalyticsField>(cancellationToken);

        var res = analyticsFields.Select(ToTagKeyRes).ToList();

        return Ok(res);
    }

    [HttpGet("tags/values")]
    public async Task<ActionResult<IEnumerable<TagValueRes>>> GetTagValues([FromQuery] string key,
                                                                          [FromQuery] string searchText,
                                                                          CancellationToken cancellationToken) {
        var analyticsFields = await _lookups.GetAllAsync<AnalyticsField>(cancellationToken);
        var analyticsField = analyticsFields.FirstOrDefault(x => x.Key.EqualsInvariant(key));

        if (analyticsField == null) {
            return NotFound();
        }

        var bearerToken = await _auth0TokenAccessor.GetAsync(UserDirectoryTypes.BackOffice);
        var client = _clientFactory.Value.Create(CloudApiTypes.Engage, bearerToken);

        var options = await client.InvokeAsync(x => GetOptionsAsync(x,
                                                                    analyticsField.Key,
                                                                    searchText,
                                                                    cancellationToken));

        var res = options.Select(ToTagValueRes).ToList();

        return Ok(res);
    }

    private async Task<IReadOnlyList<OptionsSourceValueRes>> GetOptionsAsync(TagsOptionsSourcesClient client,
                                                                             string key,
                                                                             string searchText,
                                                                             CancellationToken cancellationToken) {
        if (searchText.HasValue()) {
            var req = new SearchOptionsSourceValuesReq();
            req.Group = key;
            req.SearchText = searchText;
            req.Source = AnalyticsFieldsOptionsSource;

            var options = await client.SearchOptionsSourceValuesAsync(req, cancellationToken);

            return options.ToList();
        } else {
            var req = new OptionsSourceValuesReq();
            req.Group = key;
            req.Source = AnalyticsFieldsOptionsSource;

            var options = await client.GetCommonOptionsSourceValuesAsync(req, cancellationToken);

            return options.ToList();
        }
    }

    private TagKeyRes ToTagKeyRes(AnalyticsField analyticsField) {
        var res = new TagKeyRes();
        res.Description = analyticsField.Description;
        res.Key = analyticsField.Key;
        res.Name = analyticsField.Name;

        return res;
    }

    private TagValueRes ToTagValueRes(OptionsSourceValueRes option) {
        var res = new TagValueRes();
        res.Value = option.Value;

        return res;
    }
}