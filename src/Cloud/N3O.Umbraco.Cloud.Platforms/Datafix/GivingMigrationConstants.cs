namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public static class GivingMigrationConstants {
    public const string LiveCloudHost = "n3o.cloud";
    public const int DefaultMigrateLimit = 10;

    public static class Platforms {
        public const string CampaignsAlias = "platformsCampaigns";
    }

    public static class Properties {
        public const string AnalyticsTags = "analyticsTags";
        public const string Description = "description";
        public const string DonationItem = "donationItem";
        public const string HeroImage = "heroImage";
        public const string Icon = "icon";
        public const string Image = "image";
        public const string Summary = "summary";
    }

    public static class Outcomes {
        public const string Created = nameof(Created);
        public const string Failed = nameof(Failed);
        public const string NotAttempted = nameof(NotAttempted);
        public const string Published = nameof(Published);
    }

    public static class Legacy {
        public const string DonationFormsAlias = "donationForms";
        public const string DonationFormFolderAlias = "donationFormFolder";
        public const string FundDonationOptionAlias = "fundDonationOption";
        public const string SponsorshipDonationOptionAlias = "sponsorshipDonationOption";
        public const string FeedbackDonationOptionAlias = "feedbackDonationOption";
        public const string UpsellOfferAlias = "upsellOffer";
    }

    public static class Severities {
        public const string Blocker = "Blocker";
        public const string Warning = "Warning";
        public const string DataLoss = "DataLoss";
    }

    public static class IssueKinds {
        public const string LegacyTreeNotRecognised = nameof(LegacyTreeNotRecognised);
        public const string PlatformsTreeMissing = nameof(PlatformsTreeMissing);
        public const string TargetContentTypeMissing = nameof(TargetContentTypeMissing);
        public const string SlugCollision = nameof(SlugCollision);
        public const string SlugNotDerivable = nameof(SlugNotDerivable);
        public const string NameDisambiguated = nameof(NameDisambiguated);
        public const string NameNotDerivable = nameof(NameNotDerivable);
        public const string EmptyForm = nameof(EmptyForm);
        public const string UpsellOfferNotMigrated = nameof(UpsellOfferNotMigrated);
        public const string PartiallyMigrated = nameof(PartiallyMigrated);
    }

    public static class EntryStatuses {
        public const string Planned = nameof(Planned);
        public const string Blocked = nameof(Blocked);
        public const string AlreadyMigrated = nameof(AlreadyMigrated);
    }
}
