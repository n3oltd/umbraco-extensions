using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Media;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DonationFormContentContentExtensions {
    public static DonationFormContentReq ToDonationFormContentReq(this DonationFormContentContent src,
                                                                  IMediaUrl mediaUrl,
                                                                  IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        var donationFormContentReq = new DonationFormContentReq();
        donationFormContentReq.Summary = src.Summary;
        donationFormContentReq.Description = src.Description.ToHtmlString().ToRichTextContentReq();
        donationFormContentReq.Image = src.Image.ToImageSimpleContentReq(mediaUrl, mediaUrlBuilder);
        donationFormContentReq.Icon = src.Icon.ToSvgContentReq(mediaUrl, mediaUrlBuilder);

        return donationFormContentReq;
    }
}
