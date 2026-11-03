using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Lookups;

[Order(int.MaxValue)]
public class ApiFeedbackSchemes : ApiLookupsCollection<FeedbackScheme> {
    private readonly ICdnClient _cdnClient;
    private readonly ILogger<ApiFeedbackSchemes> _logger;

    public ApiFeedbackSchemes(ICdnClient cdnClient, ILogger<ApiFeedbackSchemes> logger) {
        _cdnClient = cdnClient;
        _logger = logger;
    }
    
    protected override async Task<IReadOnlyList<FeedbackScheme>> FetchAsync(CancellationToken cancellationToken) {
        var publishedFeedbackSchemes = await _cdnClient.DownloadSubscriptionContentAsync<PublishedFeedbackSchemes>(SubscriptionFiles.FeedbackSchemes,
                                                                                                                   JsonSerializers.JsonProvider,
                                                                                                                   cancellationToken);

        var feedbackSchemes = new List<FeedbackScheme>();

        foreach (var publishedFeedbackScheme in publishedFeedbackSchemes.OrEmpty(x => x.FeedbackSchemes)) {
            var allowedDonationPlans = publishedFeedbackScheme.AllowedDonationPlans
                                                              .OrEmpty()
                                                              .Select(x => ToDonationPlan(publishedFeedbackScheme, x))
                                                              .ExceptNull()
                                                              .ToList();

            var feedbackScheme = new FeedbackScheme(publishedFeedbackScheme.Id,
                                                    publishedFeedbackScheme.Name,
                                                    null,
                                                    publishedFeedbackScheme.AllowedGivingTypes,
                                                    allowedDonationPlans,
                                                    publishedFeedbackScheme.FundDimensionOptions.IfNotNull(x => new FundDimensionOptions(x)),
                                                    publishedFeedbackScheme.Pricing.IfNotNull(x => new Pricing(x)),
                                                    publishedFeedbackScheme.CustomFieldDefinitions.OrEmpty().Select(ToFeedbackCustomFieldDefinition));
            
            feedbackSchemes.Add(feedbackScheme);
        }

        return feedbackSchemes;
    }

    private DonationPlan ToDonationPlan(PublishedFeedbackScheme publishedFeedbackScheme,
                                        PublishedDonationPlan publishedDonationPlan) {
        var donationPlan = StaticLookups.FindById<DonationPlan>(publishedDonationPlan.Id);

        if (!donationPlan.HasValue()) {
            _logger.LogWarning("Feedback scheme {FeedbackSchemeId} offers unrecognised donation plan {DonationPlanId}",
                               publishedFeedbackScheme.Id,
                               publishedDonationPlan.Id);
        }

        return donationPlan;
    }

    private FeedbackCustomFieldDefinition ToFeedbackCustomFieldDefinition(PublishedFeedbackCustomFieldDefinition publishedFeedbackCustomFieldDefinition) {
        return new FeedbackCustomFieldDefinition(StaticLookups.FindById<FeedbackCustomFieldType>(publishedFeedbackCustomFieldDefinition.Type.Id),
                                                 publishedFeedbackCustomFieldDefinition.Alias,
                                                 publishedFeedbackCustomFieldDefinition.Name,
                                                 publishedFeedbackCustomFieldDefinition.Required,
                                                 publishedFeedbackCustomFieldDefinition.Text?.MaxLength.GetValueOrDefault() ?? 0);
    }

    protected override TimeSpan ReloadInterval => TimeSpan.FromMinutes(1);
}
