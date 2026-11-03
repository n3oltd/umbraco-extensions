using N3O.Umbraco.Attributes;
using N3O.Umbraco.Giving.Allocations.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Giving.Allocations.Models;

public class FeedbackAllocationReq : IFeedbackAllocation {
    [Name("Scheme")]
    public FeedbackScheme Scheme { get; set; }

    [Name("Donation Plan")]
    public DonationPlan DonationPlan { get; set; }

    [Name("Custom Fields")]
    public FeedbackNewCustomFieldsReq CustomFields { get; set; }

    [JsonIgnore]
    IEnumerable<IFeedbackCustomField> IFeedbackAllocation.CustomFields =>
        CustomFields.Entries.Select(x => x.ToFeedbackCustomField(Scheme));
}