using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Media;
using System.Linq;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using ECommerceStage = N3O.Umbraco.Cloud.Platforms.Clients.ECommerceStage;
using PlatformsCurrency = N3O.Umbraco.Cloud.Platforms.Clients.Currency;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class UpdateCrossSellReqMapping : IMapDefinition {
    private readonly IBaseCurrencyAccessor _baseCurrencyAccessor;
    private readonly IMediaUrl _mediaUrl;
    private readonly IPlatformsMediaUrlBuilder _mediaUrlBuilder;

    public UpdateCrossSellReqMapping(IBaseCurrencyAccessor baseCurrencyAccessor,
                                     IMediaUrl mediaUrl,
                                     IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        _baseCurrencyAccessor = baseCurrencyAccessor;
        _mediaUrl = mediaUrl;
        _mediaUrlBuilder = mediaUrlBuilder;
    }

    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<CrossSellContent, UpdateCrossSellReq>((_, _) => new UpdateCrossSellReq(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(CrossSellContent src, UpdateCrossSellReq dest, MapperContext ctx) {
        dest.Name = src.Name;
        dest.Order = src.Content().Parent().Children().FindIndex(x => x.Id == src.Content().Id);
        dest.Stage = src.Stage.ToEnum<ECommerceStage>();

        dest.Targeting = new CrossSellTargetingReq();
        dest.Targeting.Campaigns = src.TargetCampaigns.OrEmpty().Select(x => x.Id).ToList();

        if (src.TargetGiftTypes.HasAny()) {
            dest.Targeting.GiftTypes = src.TargetGiftTypes.Select(x => x.ToEnum<GiftType>().GetValueOrThrow()).ToList();
        }

        dest.RelativeValue = GetCrossSellRelativeValueReq(src);

        dest.FormContent = src.FormContent.ToDonationFormContentReq(_mediaUrl, _mediaUrlBuilder);
        dest.FormState = ctx.Map<CrossSellContent, DonationFormStateReq>(src);

        if (src.Content().IsPublished()) {
            dest.Activate = true;
        }
    }

    private CrossSellRelativeValueReq GetCrossSellRelativeValueReq(CrossSellContent src) {
        if (!src.Percentage.IsSpecified()) {
            return null;
        }

        var currency = _baseCurrencyAccessor.GetPlatformsBaseCurrency();

        var relativeValue = new CrossSellRelativeValueReq();
        relativeValue.Percentage = (double) src.Percentage.ToFraction();
        relativeValue.Minimum = GetMoneyReq(src.Minimum, currency);
        relativeValue.Maximum = GetMoneyReq(src.Maximum, currency);
        relativeValue.Threshold = GetMoneyReq(src.Threshold, currency);

        return relativeValue;
    }

    private MoneyReq GetMoneyReq(decimal amount, PlatformsCurrency? currency) {
        if (!amount.IsSpecified()) {
            return null;
        }

        return ((double) amount).ToMoneyReq(currency);
    }
}
