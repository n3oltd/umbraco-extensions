using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using System.Threading.Tasks;
using AllocationType = N3O.Umbraco.Cloud.Platforms.Clients.AllocationType;
using FundDimensionValuesReq = N3O.Umbraco.Cloud.Platforms.Clients.FundDimensionValuesReq;
using PlatformsCurrency = N3O.Umbraco.Cloud.Platforms.Clients.Currency;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class AllocationIntentReqExtensions {
    public static async Task<AllocationIntentReq> ToAllocationIntentReqAsync(this DonationFormStateContent formState,
                                                                             ICdnClient cdnClient,
                                                                             PlatformsCurrency? currency,
                                                                             double amount) {
        var fundDimensionValues = formState.GetFixedFundDimensionValues();
        
        var allocationIntent = new AllocationIntentReq();
        allocationIntent.Type = formState.Type.ToEnum<AllocationType>();

        allocationIntent.FundDimensions = new FundDimensionValuesReq();
        allocationIntent.FundDimensions.Dimension1 = fundDimensionValues.Dimension1?.Name;
        allocationIntent.FundDimensions.Dimension2 = fundDimensionValues.Dimension2?.Name;
        allocationIntent.FundDimensions.Dimension3 = fundDimensionValues.Dimension3?.Name;
        allocationIntent.FundDimensions.Dimension4 = fundDimensionValues.Dimension4?.Name;

        allocationIntent.Value = new MoneyReq();
        allocationIntent.Value.Amount = amount;
        allocationIntent.Value.Currency = currency;

        if (formState.Type == AllocationTypes.Fund) {
            allocationIntent.Fund = new FundIntentReq();
            allocationIntent.Fund.DonationItem = formState.Fund.DonationItem.Name;
        } else if (formState.Type == AllocationTypes.Feedback) {
            allocationIntent.Feedback = new FeedbackIntentReq();
            allocationIntent.Feedback.New = new NewFeedbackIntentReq();
            allocationIntent.Feedback.New.Scheme = formState.Feedback.Scheme.Name;
        } else if (formState.Type == AllocationTypes.Qurbani) {
            var season = await cdnClient.DownloadSubscriptionContentAsync<PublishedQurbaniSeason>(SubscriptionFiles.QurbaniSeason, JsonSerializers.JsonProvider);
            
            allocationIntent.Qurbani = new QurbaniIntentReq();
            allocationIntent.Qurbani.New = new NewQurbaniIntentReq();
            allocationIntent.Qurbani.New.Item = formState.Qurbani.QurbaniItem.Name;
            allocationIntent.Qurbani.New.Season = season.Name;
        } else if (formState.Type == AllocationTypes.Sponsorship) {
            allocationIntent.Sponsorship = new SponsorshipIntentReq();
            allocationIntent.Sponsorship.New = new NewSponsorshipIntentReq();
            allocationIntent.Sponsorship.New.Scheme = formState.Sponsorship.Scheme.Name;
        }

        return allocationIntent;
    }
}
