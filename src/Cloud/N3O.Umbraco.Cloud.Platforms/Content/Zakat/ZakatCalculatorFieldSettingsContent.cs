using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;

namespace N3O.Umbraco.Cloud.Platforms.Content;

[UmbracoContent(PlatformsConstants.Zakat.Settings.Calculator.Field.Alias)]
public class ZakatCalculatorFieldSettingsContent : UmbracoContent<ZakatCalculatorFieldSettingsContent> {
    public ZakatCalculatorFieldClassification Classification => GetValue(x => x.Classification);
    public ZakatCalculatorFieldType Type => GetValue(x => x.Type);
    public string Alias => GetValue(x => x.Alias);
    public string Name => Content().Name;
    public new object Content => GetValue(x => x.Content);
    public string Tooltip => GetValue(x => x.Tooltip);
    public Metal Metal => GetValue(x => x.Metal);
}