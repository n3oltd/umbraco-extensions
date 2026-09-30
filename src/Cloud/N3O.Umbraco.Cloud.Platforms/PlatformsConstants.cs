namespace N3O.Umbraco.Cloud.Platforms;

public static class PlatformsConstants {
    public const string BackOfficeApiName = "PlatformsBackOffice";
    public const string DevToolsApiName = "PlatformsDevTools";

    public static class Campaigns {
        public const string CompositionAlias = "platformsCampaign";
        public const string Qurbani = "platformsQurbaniCampaign";
        public const string Giving = "platformsGivingCampaign";
        public const string Standard = "platformsStandardCampaign";
        public const string Telethon = "platformsTelethonCampaign";
        
        public const string ScheduledGiving = "platformsScheduledGivingCampaign";
        public const string RegularGiving = "platformsRegularGivingCampaign";
    }

    public static class CrossSells {
        public const string CompositionAlias = "platformsCrossSell";
        public const string Feedback = "platformsFeedbackCrossSell";
        public const string Fund = "platformsFundCrossSell";
        public const string Qurbani = "platformsQurbaniCrossSell";
        public const string Sponsorship = "platformsSponsorshipCrossSell";
    }

    public static class CrowdfundingCampaigns {
        public const string Alias = "platformsCrowdfundingCampaigns";

        public static class CrowdfundingCampaign {
            public const string Alias = "platformsCrowdfundingCampaign";

            public static class Properties {
                public const string Campaign = "campaign";
            }
        }
    }

    public static class DonationFormContent {
        public const string CompositionAlias = "platformsDonationFormContent";
    }

    public static class DonationFormState {
        public const string CompositionAlias = "platformsDonationFormState";
        public const string Feedback = "platformsFeedbackDonationFormState";
        public const string Fund = "platformsFundDonationFormState";
        public const string Qurbani = "platformsQurbaniDonationFormState";
        public const string Sponsorship = "platformsSponsorshipDonationFormState";
        public const string SuggestedAmount = "platformsDonationFormStateSuggestedAmount";
    }

    public static class Feeds {
        public const string Alias = "feeds";

        public static class Feed {
            public const string Alias = "feed";
        }

        public static class Item {
            public const string Alias = "feedsItem";
        }

        public static class Folders {
            public const string ApprovedFolder = "feedsApprovedFolder";
            public const string RejectedFolder = "feedsRejectedFolder";
            public const string ArchivedFolder = "feedsArchivedFolder";
        }
    }

    public static class Offerings {
        public const string CompositionAlias = "platformsOffering";
        public const string Feedback = "platformsFeedbackOffering";
        public const string Fund = "platformsFundOffering";
        public const string Qurbani = "platformsQurbaniOffering";
        public const string Sponsorship = "platformsSponsorshipOffering";
    }

    public static class Platforms {
        public const string Alias = "platforms";
    }

    public static class Qurbani {
        public static class Season {
            public const string Alias = "platformsQurbaniSeason";

            public static class Category {
                public const string Alias = "platformsQurbaniSeasonCategory";
            }
        }
    }

    public static class Webhooks {
        public static class EventTypes {
            public static class Crowdfunder {
                public const string Created = "crowdfunder.created";
                public const string Updated = "crowdfunder.updated";
            }
        }

        public static class HookIds {
            public const string Campaigns = "campaigns";
            public const string ContentCollection = "contentCollection";
            public const string ContentLibrary = "contentLibrary";
            public const string CrossSells = "crossSells";
            public const string Crowdfunder = nameof(Crowdfunder);
            public const string CrowdfundingCampaigns = "crowdfundingCampaigns";
            public const string ManagedContent = "managedContent";
            public const string Offerings = "offerings";
            public const string QurbaniSeason = "qurbaniSeason";
            public const string ZakatSettings = "zakatSettings";
        }
    }

    public static class Zakat {
        public static class Settings {
            public static class Calculator {
                public const string Alias = "zakatCalculatorSettings";

                public static class Field {
                    public const string Alias = "zakatCalculatorFieldSettings";
                }

                public static class Section {
                    public const string Alias = "zakatCalculatorSectionSettings";
                }
            }
        }
    }
}
