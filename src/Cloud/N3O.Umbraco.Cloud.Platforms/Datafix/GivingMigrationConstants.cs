namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public static class GivingMigrationConstants {
    public const int DefaultMigrateLimit = 10;
    public const string NameSeparator = " - ";
    public const string PathSeparator = " / ";

    public static class EntryStatuses {
        public const string AlreadyMigrated = nameof(AlreadyMigrated);
        public const string Blocked = nameof(Blocked);
        public const string Planned = nameof(Planned);
    }

    public static class IssueKinds {
        public const string AmbiguousReference = nameof(AmbiguousReference);
        public const string DroppedProperty = nameof(DroppedProperty);
        public const string EmptyForm = nameof(EmptyForm);
        public const string LegacyTreeNotRecognised = nameof(LegacyTreeNotRecognised);
        public const string NameDisambiguated = nameof(NameDisambiguated);
        public const string NameNotDerivable = nameof(NameNotDerivable);
        public const string NotRepointed = nameof(NotRepointed);
        public const string PendingDraft = nameof(PendingDraft);
        public const string PlatformsTreeMissing = nameof(PlatformsTreeMissing);
        public const string ResidualReference = nameof(ResidualReference);
        public const string TargetContentTypeMissing = nameof(TargetContentTypeMissing);
        public const string UnmappedReference = nameof(UnmappedReference);
        public const string UnrecognisedReference = nameof(UnrecognisedReference);
    }

    public static class KeyValueKeys {
        public const string Ledger = "n3o/givingMigration/ledger";
        public const string PersistedPlan = "n3o/givingMigration/persistedPlan";
        public const string Placeholders = "n3o/givingMigration/placeholders";
        public const string TreeLockSnapshot = "n3o/givingMigration/treeLockSnapshot";
    }

    public static class LedgerKinds {
        public const string Campaign = nameof(Campaign);
        public const string CrossSell = nameof(CrossSell);
        public const string Offering = nameof(Offering);
    }

    public static class Legacy {
        public const string DonationFormAlias = "donationForm";
        public const string DonationFormFolderAlias = "donationFormFolder";
        public const string DonationFormsAlias = "donationForms";
        public const string FeedbackDonationOptionAlias = "feedbackDonationOption";
        public const string FundDonationOptionAlias = "fundDonationOption";
        public const string PriceHandleAlias = "priceHandle";
        public const string SponsorshipDonationOptionAlias = "sponsorshipDonationOption";
        public const string UpsellOfferAlias = "upsellOffer";
    }

    public static class Outcomes {
        public const string Created = nameof(Created);
        public const string Failed = nameof(Failed);
        public const string NotAttempted = nameof(NotAttempted);
        public const string Published = nameof(Published);
        public const string Purged = nameof(Purged);
        public const string Rewritten = nameof(Rewritten);
        public const string Skipped = nameof(Skipped);
    }

    public static class Placeholders {
        public const string AnalyticsTagName = "campaign";
        public const string IconFilename = "giving-migration-icon.svg";

        // The icon picker only accepts vector graphics, and placehold.co serves image/svg+xml by default
        public const string IconUrl = "https://placehold.co/256.svg";

        public const string ImageFilename = "giving-migration-image.jpg";
        public const string ImageUrl = "https://picsum.photos/1600/900";
        public const string MediaFolderName = "Giving Migration";
    }

    // Everything else the migration targets is already named in PlatformsConstants. The campaigns container is
    // not, because the seeder does not create it.
    public static class Platforms {
        public const string CampaignsAlias = "platformsCampaigns";
    }

    public static class Properties {
        public const string Amount = "amount";
        public const string AnalyticsTags = "analyticsTags";
        public const string DefaultGivingType = "defaultGivingType";
        public const string Description = "description";
        public const string Dimension1 = "dimension1";
        public const string Dimension2 = "dimension2";
        public const string Dimension3 = "dimension3";
        public const string DonationItem = "donationItem";
        public const string DonationPriceHandles = "donationPriceHandles";
        public const string FixedAmount = "fixedAmount";
        public const string GivingType = "givingType";
        public const string HeroImage = "heroImage";
        public const string HideDonation = "hideDonation";
        public const string HideQuantity = "hideQuantity";
        public const string HideRegularGiving = "hideRegularGiving";
        public const string Icon = "icon";
        public const string Image = "image";
        public const string OneTimeSuggestedAmounts = "oneTimeSuggestedAmounts";
        public const string PriceHandles = "priceHandles";
        public const string RecurringSuggestedAmounts = "recurringSuggestedAmounts";
        public const string RegularGivingPriceHandles = "regularGivingPriceHandles";
        public const string Scheme = "scheme";
        public const string Stage = "stage";
        public const string SuggestedGiftType = "suggestedGiftType";
        public const string Summary = "summary";
    }

    public static class Severities {
        public const string Blocker = "Blocker";
        public const string DataLoss = "DataLoss";
        public const string Warning = "Warning";
    }

    public static class Stages {
        public const string Cart = "cart";
    }
}
