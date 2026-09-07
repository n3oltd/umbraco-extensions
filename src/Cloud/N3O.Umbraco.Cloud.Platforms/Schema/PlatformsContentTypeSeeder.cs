using Microsoft.Extensions.Logging;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsContentTypeSeeder : IPlatformsContentTypeSeeder {
    private readonly IContentTypeEditor _contentTypeEditor;
    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly ILogger<PlatformsContentTypeSeeder> _logger;

    public PlatformsContentTypeSeeder(IContentTypeEditor contentTypeEditor,
                                      IDataTypeEditor dataTypeEditor,
                                      ILogger<PlatformsContentTypeSeeder> logger) {
        _contentTypeEditor = contentTypeEditor;
        _dataTypeEditor = dataTypeEditor;
        _logger = logger;
    }

    public IReadOnlyList<string> Aliases => GetSeeds().Select(x => x.Alias).ToList();

    public void Seed() {
        foreach (var seed in GetSeeds()) {
            Seed(seed.Alias, seed.Run);
        }
    }

    private IReadOnlyList<(string Alias, Action Run)> GetSeeds() {
        return [
            (PlatformsConstants.DonationFormState.SuggestedAmount, SeedSuggestedAmount),
            (PlatformsConstants.DonationFormContent.CompositionAlias, SeedDonationFormContent),
            (PlatformsConstants.DonationFormState.CompositionAlias, SeedDonationFormState),
            (PlatformsConstants.DonationFormState.Feedback, SeedFeedbackDonationFormState),
            (PlatformsConstants.DonationFormState.Fund, SeedFundDonationFormState),
            (PlatformsConstants.DonationFormState.Qurbani, SeedQurbaniDonationFormState),
            (PlatformsConstants.DonationFormState.Sponsorship, SeedSponsorshipDonationFormState),

            (PlatformsConstants.Platforms.Alias, SeedPlatforms),

            (PlatformsConstants.Campaigns.CompositionAlias, SeedCampaign),
            (PlatformsConstants.Campaigns.Standard, SeedStandardCampaign),
            (PlatformsConstants.Campaigns.Giving, SeedGivingCampaign),
            (PlatformsConstants.Campaigns.Qurbani, SeedQurbaniCampaign),
            (PlatformsConstants.Campaigns.Telethon, SeedTelethonCampaign),
            (PlatformsConstants.Campaigns.ScheduledGiving, SeedScheduledGivingCampaign),
            (PlatformsConstants.Campaigns.RegularGiving, SeedRegularGivingCampaign),
            (PlatformsConstants.Offerings.CompositionAlias, SeedOffering),
            (PlatformsConstants.CrossSells.CompositionAlias, SeedCrossSell),
            (PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias, SeedCrowdfundingCampaign),
            (PlatformsConstants.CrowdfundingCampaigns.Alias, SeedCrowdfundingCampaigns),
            (PlatformsConstants.Qurbani.Season.Category.Alias, SeedQurbaniSeasonCategory),
            (PlatformsConstants.Qurbani.Season.Alias, SeedQurbaniSeason),

            (PlatformsConstants.Zakat.Settings.Calculator.Field.Alias, SeedZakatCalculatorField),
            (PlatformsConstants.Zakat.Settings.Calculator.Section.Alias, SeedZakatCalculatorSection),
            (PlatformsConstants.Zakat.Settings.Calculator.Alias, SeedZakatCalculatorSettings)
        ];
    }

    private bool HasDataType(string name) {
        return _dataTypeEditor.Find(name) != null;
    }

    private void Seed(string alias, Action seed) {
        if (_contentTypeEditor.Find(alias) != null) {
            return;
        }

        try {
            seed();
        } catch (Exception ex) when (ex is DataTypeNotFoundException or ContentTypeNotFoundException) {
            _logger.LogInformation("Deferred platforms content type {Alias}: {Reason}", alias, ex.Message);
        } catch (ContentTypeKindMismatchException ex) {
            _logger.LogWarning("Platforms content type {Alias} disagrees with the site: {Reason}",
                               alias,
                               ex.Message);
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not create platforms content type {Alias}", alias);
        }
    }
}
