using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Hosting;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

public class PlatformsLinksVisibilityController : WorkspaceVisibilityController {
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;

    public PlatformsLinksVisibilityController(IContentService contentService, IContentTypeService contentTypeService) {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    protected override Task<bool> IsVisibleAsync(Guid contentId) {
        var content = _contentService.GetById(contentId);

        return Task.FromResult(content == null || !content.HasPlatformsUrl(_contentTypeService));
    }
}
