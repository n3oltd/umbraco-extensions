using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class ZakatCalculatorSectionValidator : ContentValidator {
    private static readonly string AliasPropertyAlias = AliasHelper<ZakatCalculatorSectionSettingsContent>.PropertyAlias(x => x.Alias);

    public ZakatCalculatorSectionValidator(IContentHelper contentHelper) : base(contentHelper) { }

    public override bool IsValidator(ContentProperties content) {
        return content.ContentTypeAlias.EqualsInvariant(PlatformsConstants.Zakat.Settings.Calculator.Section.Alias);
    }

    public override void Validate(ContentProperties content) {
        var aliasProperty = content.GetPropertyByAlias(AliasPropertyAlias);

        if (ContentHelper.AnotherSiblingHasSameValue(content, aliasProperty)) {
            ErrorResult(aliasProperty, "must be unique within the calculator");
        }
    }
}
