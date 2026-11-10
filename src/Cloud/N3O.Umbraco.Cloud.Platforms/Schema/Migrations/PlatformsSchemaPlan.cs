using System.Collections.Generic;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;
using States = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Migrations;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsSchemaPlan : MigrationPlan {
    public PlatformsSchemaPlan() : base(States.PlanName) {
        // V5 is v17-only. A step ported from main after it needs a path from that step's main
        // state that still runs V5, or a site upgrading from v13 at that state skips it.
        From(string.Empty).To<PlatformsContentTypesMigrationV2>(States.PlatformsContentTypesMigrationV2)
                          .To<PlatformsContentTypesMigrationV3>(States.PlatformsContentTypesMigrationV3)
                          .To<PlatformsContentTypesMigrationV4>(States.PlatformsContentTypesMigrationV4)
                          .To<PlatformsDataTypesMigrationV5>(States.PlatformsDataTypesMigrationV5)
                          .To<PlatformsContentTypesMigrationV6>(States.PlatformsContentTypesMigrationV6);
    }

    public static IReadOnlyList<string> RequiredContentTypes => [
        PlatformsConstants.Campaigns.CompositionAlias,
        PlatformsConstants.CrossSells.CompositionAlias,
        PlatformsConstants.DonationFormState.SuggestedAmount,
        PlatformsConstants.Offerings.CompositionAlias
    ];

    public static IReadOnlyList<string> RequiredDataTypes => [
        DataTypeNames.ElementEmbedCodeLabel,
        DataTypeNames.FundraiserTag,
        DataTypeNames.GiftTypesMultiple,
        DataTypeNames.Percentage,
        Shared.Money,
        DataTypeNames.SuggestedAmountTitle
    ];
}
