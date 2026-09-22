using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Templates.Models;
using N3O.Umbraco.Json;
using N3O.Umbraco.Pages;
using N3O.Umbraco.Templates.Extensions;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Templates.Extensions;

public static class PageViewModelExtensions {
    public static PublishedCampaign Campaign(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return PublishedFile<PublishedCampaign, PlatformsPublishedFileKind>(pageViewModel,
                                                                            jsonProvider, 
                                                                            PlatformsPublishedFileKind.Campaign);
    }

    public static PublishedCampaignPage CampaignPage(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return PublishedFile<PublishedCampaignPage, PlatformsPublishedFileKind>(pageViewModel,
                                                                                jsonProvider,
                                                                                PlatformsPublishedFileKind.CampaignPage);
    }
    
    public static IEnumerable<PublishedCampaign> Campaigns(this IPageViewModel pageViewModel,
                                                           IJsonProvider jsonProvider) {
        return pageViewModel.MergeModel<IEnumerable<PublishedCampaign>>(jsonProvider,
                                                                        PlatformsTemplateConstants.ModelKeys.Campaigns);
    }
    
    public static IEnumerable<PublishedOffering> CampaignOfferings(this IPageViewModel pageViewModel,
                                                                   IJsonProvider jsonProvider) {
        return pageViewModel.MergeModel<IEnumerable<PublishedOffering>>(jsonProvider,
                                                                        PlatformsTemplateConstants.ModelKeys.CampaignOfferings);
    }
    
    public static PublishedCrowdfunderPage CrowdfunderPage(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return PublishedFile<PublishedCrowdfunderPage, CrowdfundingPublishedFileKind>(pageViewModel,
                                                                                      jsonProvider,
                                                                                      CrowdfundingPublishedFileKind.CrowdfunderPage);
    }

    public static PublishedCrowdfundingCampaignPage CrowdfundingCampaignPage(this IPageViewModel pageViewModel,
                                                                             IJsonProvider jsonProvider) {
        return PublishedFile<PublishedCrowdfundingCampaignPage, CrowdfundingPublishedFileKind>(pageViewModel,
                                                                                               jsonProvider,
                                                                                               CrowdfundingPublishedFileKind.CrowdfundingCampaignPage);
    }

    public static NisabMergeModel Nisab(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return pageViewModel.MergeModel<NisabMergeModel>(jsonProvider, PlatformsTemplateConstants.ModelKeys.Nisab);
    }
    
    public static PublishedOffering Offering(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return PublishedFile<PublishedOffering, PlatformsPublishedFileKind>(pageViewModel,
                                                                            jsonProvider, 
                                                                            PlatformsPublishedFileKind.Offering);
    }
    
    public static PublishedOfferingPage OfferingPage(this IPageViewModel pageViewModel, IJsonProvider jsonProvider) {
        return PublishedFile<PublishedOfferingPage, PlatformsPublishedFileKind>(pageViewModel,
                                                                                jsonProvider, 
                                                                                PlatformsPublishedFileKind.OfferingPage);
    }
    
    public static T PublishedFile<T, U>(this IPageViewModel pageViewModel,
                                        IJsonProvider jsonProvider,
                                        U kind) where U : struct, Enum {
        return pageViewModel.MergeModel<T>(jsonProvider, kind.ToEnumString());
    }
    
    public static PlatformsUserRes User(this IPageViewModel pageViewModel,
                                        IJsonProvider jsonProvider) {
        return pageViewModel.MergeModel<PlatformsUserRes>(jsonProvider, PlatformsTemplateConstants.ModelKeys.User);
    }
}
