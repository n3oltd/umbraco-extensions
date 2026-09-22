using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Content;

[UmbracoContent(PlatformsConstants.Zakat.Settings.Calculator.Section.Alias)]
public class ZakatCalculatorSectionSettingsContent : UmbracoContent<ZakatCalculatorSectionSettingsContent> {
    public string Alias => GetValue(x => x.Alias);
    public string Name => Content().Name;
    public new object Content => GetValue(x => x.Content);
    public IReadOnlyList<ZakatCalculatorFieldSettingsContent> Fields =>
        Content().Children.As<ZakatCalculatorFieldSettingsContent>();
}
