using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using NodaTime.Extensions;
using NodaTime.Text;
using Umbraco.Cms.Core.Mapping;
using CampaignType = N3O.Umbraco.Cloud.Platforms.Clients.CampaignType;
using GivingType = N3O.Umbraco.Cloud.Platforms.Clients.GivingType;
using RegularGivingFrequency = N3O.Umbraco.Cloud.Platforms.Clients.RegularGivingFrequency;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class CreateCampaignReqMapping : IMapDefinition {
    private readonly ICdnClient _cdnClient;

    public CreateCampaignReqMapping(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }
    
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<CampaignContent, CreateCampaignReq>((_, _) => new CreateCampaignReq(), Map);
    }
    
    // Umbraco.Code.MapAll
    private void Map(CampaignContent src, CreateCampaignReq dest, MapperContext ctx) {
        dest.Type = src.Type.ToEnum<CampaignType>();
        dest.Name = src.Name;
        dest.Notes = src.Notes;
        
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