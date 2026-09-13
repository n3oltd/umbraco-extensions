using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Media;
using N3O.Umbraco.Utilities;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DonationFormContentContentExtensions {
    public static DonationFormContentReq ToDonationFormContentReq(this DonationFormContentContent src,
                                                                  IMediaUrl mediaUrl,
                                                                  IUrlBuilder urlBuilder) {
        var donationFormContentReq = new DonationFormContentReq();
        donationFormContentReq.Summary = src.Summary;
        donationFormContentReq.Description = src.Description.ToHtmlString().ToRichTextContentReq();
        donationFormContentReq.Image = src.Image.ToImageSimpleContentReq(mediaUrl, urlBuilder);
        donationFormContentReq.Icon = src.Icon.ToSvgContentReq(mediaUrl, urlBuilder);

        return donationFormContentReq;
    }
}
