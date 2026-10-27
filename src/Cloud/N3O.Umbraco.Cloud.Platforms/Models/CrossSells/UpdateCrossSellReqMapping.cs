using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Media;
using System.Linq;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;
using ECommerceStage = N3O.Umbraco.Cloud.Platforms.Clients.ECommerceStage;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class UpdateCrossSellReqMapping : IMapDefinition {
    private readonly IMediaUrl _mediaUrl;
    private readonly IPlatformsMediaUrlBuilder _mediaUrlBuilder;

    public UpdateCrossSellReqMapping(IMediaUrl mediaUrl, IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        _mediaUrl = mediaUrl;
        _mediaUrlBuilder = mediaUrlBuilder;
    }

    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<CrossSellContent, UpdateCrossSellReq>((_, _) => new UpdateCrossSellReq(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(CrossSellContent src, UpdateCrossSellReq dest, MapperContext ctx) {
        dest.Name = src.Name;
        dest.Order = src.Content().Parent.Children.FindIndex(x => x.Id == src.Content().Id);
        dest.Stage = src.Stage.ToEnum<ECommerceStage>();

        dest.Targeting = new CrossSellTargetingReq();
        dest.Targeting.Campaigns = src.TargetCampaigns.OrEmpty().Select(x => x.Id).ToList();

        if (src.TargetGiftTypes.HasAny()) {
            dest.Targeting.GiftTypes = src.TargetGiftTypes.Select(x => x.ToEnum<GiftType>().GetValueOrThrow()).ToList();
        }

        dest.Targeting.Threshold = GetAmount(src.TargetThreshold);

        dest.RelativeValue = GetCrossSellRelativeValueReq(src);

        dest.FormContent = src.FormContent.ToDonationFormContentReq(_mediaUrl, _mediaUrlBuilder);
        dest.FormState = ctx.Map<CrossSellContent, DonationFormStateReq>(src);

        if (src.Content().IsPublished()) {
            dest.Activate = true;
        }
    }

    private CrossSellRelativeValueReq GetCrossSellRelativeValueReq(CrossSellContent src) {
        if (!src.Percentage.IsNonZero()) {
            return null;
        }

        var relativeValue = new CrossSellRelativeValueReq();
        relativeValue.Percentage = (double) (src.Percentage / 100m);
        relativeValue.Minimum = GetAmount(src.Minimum);
        relativeValue.Maximum = GetAmount(src.Maximum);

        return relativeValue;
    }

    private double? GetAmount(decimal amount) {
        if (!amount.IsNonZero()) {
            return null;
        }

        return (double) amount;
    }
}
