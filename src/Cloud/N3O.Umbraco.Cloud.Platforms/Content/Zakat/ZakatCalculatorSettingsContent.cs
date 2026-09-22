using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Content;

[UmbracoContent(PlatformsConstants.Zakat.Settings.Calculator.Alias)]
public class ZakatCalculatorSettingsContent : UmbracoContent<ZakatCalculatorSettingsContent> {
    public object DefaultContent => GetValue(x => x.DefaultContent);
    public string EmailCompositionId => GetValue(x => x.EmailCompositionId);
    public NisabType DefaultNisabType => GetValue(x => x.DefaultNisabType);
    public OfferingContent Offering => GetPickedAs(x => x.Offering);
    public FundDimension1Value FundDimension1 => GetValue(x => x.FundDimension1);
    public FundDimension2Value FundDimension2 => GetValue(x => x.FundDimension2);
    public FundDimension3Value FundDimension3 => GetValue(x => x.FundDimension3);
    public FundDimension4Value FundDimension4 => GetValue(x => x.FundDimension4);
    public IReadOnlyList<ZakatCalculatorSectionSettingsContent> Sections =>
        Content().Children.As<ZakatCalculatorSectionSettingsContent>();
}
