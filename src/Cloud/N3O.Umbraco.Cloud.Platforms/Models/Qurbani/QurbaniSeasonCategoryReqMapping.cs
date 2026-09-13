using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Media;
using N3O.Umbraco.Utilities;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class QurbaniSeasonCategoryReqMapping : IMapDefinition {
    private readonly IMediaUrl _mediaUrl;
    private readonly IUrlBuilder _urlBuilder;

    public QurbaniSeasonCategoryReqMapping(IMediaUrl mediaUrl, IUrlBuilder urlBuilder) {
        _mediaUrl = mediaUrl;
        _urlBuilder = urlBuilder;
    }

    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<QurbaniSeasonCategoryContent, QurbaniSeasonCategoryReq>((_, _) => new QurbaniSeasonCategoryReq(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(QurbaniSeasonCategoryContent src, QurbaniSeasonCategoryReq dest, MapperContext ctx) {
        dest.Id = src.Content().Key.ToString();
        dest.Name = src.Name;
        dest.Summary = src.Summary;
        dest.Icon = src.Icon.ToSvgContentReq(_mediaUrl, _urlBuilder);
    }
}