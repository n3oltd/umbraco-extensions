using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Media;
using N3O.Umbraco.Utilities;
using Slugify;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class UpdateOfferingReqMapping : IMapDefinition {
    public const string PageContentContext = nameof(PageContentContext);
    
    private readonly IMediaUrl _mediaUrl;
    private readonly ISlugHelper _slugHelper;
    private readonly IUrlBuilder _urlBuilder;

    public UpdateOfferingReqMapping(IMediaUrl mediaUrl, ISlugHelper slugHelper, IUrlBuilder urlBuilder) {
        _mediaUrl = mediaUrl;
        _slugHelper = slugHelper;
        _urlBuilder = urlBuilder;
    }
    
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<OfferingContent, UpdateOfferingReq>((_, _) => new UpdateOfferingReq(), Map);
    }
    
    // Umbraco.Code.MapAll
    private void Map(OfferingContent src, UpdateOfferingReq dest, MapperContext ctx) {
        dest.Name = src.Name;
        dest.Notes = src.Notes;
        dest.Slug = _slugHelper.GenerateSlug(src.Name);

        dest.FormContent = src.FormContent.ToDonationFormContentReq(_mediaUrl, _urlBuilder);
        
        dest.Order = new OfferingOrderReq();
        dest.Order.Order = src.Content().Parent.Children.FindIndex(x => x.Id == src.Content().Id);
        
        try {
            ctx.Map<OfferingContent, IEnumerable<string>>(src);
        } catch {
            dest.Badges = [];
        }

        dest.Page = new ContentReq();
        dest.Page.SchemaAlias = PlatformsSystemSchema.Sys__offeringPage.ToEnumString();
        
        if (ctx.Items.TryGetValue(PageContentContext, out var value)) {
            var properties = ((IEnumerable<PropertyContentReq>) value).OrEmpty().Where(x => x.EditorHasValue());
            
            dest.Page.Properties = properties.ToList();
        }
        
        dest.FormState = ctx.Map<OfferingContent, DonationFormStateReq>(src);
        
        dest.Options = new OfferingOptionsReq();
        dest.Options.AllowCrowdfunding = src.AllowCrowdfunding;

        if (src.Content().IsPublished()) {
            dest.Activate = true;
        }
    }
}