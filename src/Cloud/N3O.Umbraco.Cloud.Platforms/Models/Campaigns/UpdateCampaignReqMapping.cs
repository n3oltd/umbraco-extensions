using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Media;
using NodaTime.Extensions;
using NodaTime.Text;
using Slugify;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;
using GivingType = N3O.Umbraco.Cloud.Platforms.Clients.GivingType;
using RegularGivingFrequency = N3O.Umbraco.Cloud.Platforms.Clients.RegularGivingFrequency;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class UpdateCampaignReqMapping : IMapDefinition {
    public const string PageContentContext = nameof(PageContentContext);
    
    private readonly ICdnClient _cdnClient;
    private readonly IMediaUrl _mediaUrl;
    private readonly IPlatformsMediaUrlBuilder _mediaUrlBuilder;
    private readonly ISlugHelper _slugHelper;

    public UpdateCampaignReqMapping(ICdnClient cdnClient,
                                    IMediaUrl mediaUrl,
                                    IPlatformsMediaUrlBuilder mediaUrlBuilder,
                                    ISlugHelper slugHelper) {
        _cdnClient = cdnClient;
        _mediaUrl = mediaUrl;
        _mediaUrlBuilder = mediaUrlBuilder;
        _slugHelper = slugHelper;
    }
    
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<CampaignContent, UpdateCampaignReq>((_, _) => new UpdateCampaignReq(), Map);
    }
    
    // Umbraco.Code.MapAll
    private void Map(CampaignContent src, UpdateCampaignReq dest, MapperContext ctx) {
        var target = (double) src.Target;
        
        dest.Name = src.Name;
        dest.Notes = src.Notes;
        dest.Slug = _slugHelper.GenerateSlug(src.Name);
        dest.Target = target == 0 ? null : target;

        dest.FormContent = src.FormContent.ToDonationFormContentReq(_mediaUrl, _mediaUrlBuilder);

        dest.Order = new CampaignOrderReq();
        dest.Order.Order = src.Content().Parent.Children.FindIndex(x => x.Id == src.Content().Id);

        try {
            ctx.Map<CampaignContent, IEnumerable<string>>(src);
        } catch {
            dest.Badges = [];
        }
        
        dest.Page = new ContentReq();
        dest.Page.SchemaAlias = PlatformsSystemSchema.Sys__campaignPage.ToEnumString();
        
        if (ctx.Items.TryGetValue(PageContentContext, out var value)) {
            var properties = ((IEnumerable<PropertyContentReq>) value).OrEmpty().Where(x => x.EditorHasValue());
            
            dest.Page.Properties = properties.ToList();
        }

        if (src.Content().IsPublished()) {
            dest.Activate = true;
        }
        
        if (src.Type == CampaignTypes.Qurbani) {
            var activeSeason = _cdnClient.DownloadSubscriptionContentAsync<PublishedQurbaniSeason>(SubscriptionFiles.QurbaniSeason,
                                                                                                   JsonSerializers.JsonProvider)
                                         .GetAwaiter().GetResult();
            
            dest.Qurbani = new QurbaniCampaignOptionsReq();
            dest.Qurbani.SeasonId = activeSeason.Id;
            dest.Qurbani.Begin = LocalDatePattern.Iso.Format(src.Qurbani.BeginAt.ToLocalDate());
            dest.Qurbani.End = LocalDatePattern.Iso.Format(src.Qurbani.EndAt.ToLocalDate());
        } else if (src.Type == CampaignTypes.Giving) {
            dest.Giving = new ConnectGivingOptionsReq();
            dest.Giving.Type = src.Giving.Type;
            dest.Giving.PaymentConfirmations = false;
            
            if (src.Giving.Type == GivingType.Regular) {
                dest.Giving.Regular = new ConnectRegularGivingOptionsReq();
                dest.Giving.Regular.Frequency = src.Giving.RegularGiving.RegularGivingFrequency.ToEnum<RegularGivingFrequency>();
                
                if (src.Giving.RegularGiving.RegularGivingFrequency == RegularGivingFrequencies.Monthly && 
                    src.Giving.RegularGiving.DayOfMonth.HasValue()) {
                    dest.Giving.Regular.Monthly = new MonthlyGivingOptionsReq();
                    dest.Giving.Regular.Monthly.CollectionDay = src.Giving.RegularGiving.DayOfMonth.Id.FromEnumString<DayOfMonth>();
                } else if (src.Giving.RegularGiving.RegularGivingFrequency == RegularGivingFrequencies.Weekly && 
                           src.Giving.RegularGiving.DayOfWeek.HasValue()) {
                    dest.Giving.Regular.Weekly = new WeeklyGivingOptionsReq();
                    dest.Giving.Regular.Weekly.CollectionDay = src.Giving.RegularGiving.DayOfWeek.ToEnum<DayOfWeek>();
                }
            } else if (src.Giving.Type == GivingType.Scheduled) {
                dest.Giving.Scheduled = new ConnectScheduledGivingOptionsReq();
                dest.Giving.Scheduled.ScheduleId = src.Giving.ScheduledGiving.Schedule.Id;
            } else {
                throw UnrecognisedValueException.For(src.Giving.Type);
            }
        } else if (src.Type == CampaignTypes.Telethon) {
            dest.Telethon = new TelethonCampaignOptionsReq();
            
            dest.Telethon.Begin = src.Telethon.BeginAt.ToLocalDateTime().ToString("o", null);
            dest.Telethon.End = src.Telethon.EndAt.ToLocalDateTime().ToString("o", null);
        }
    }
}