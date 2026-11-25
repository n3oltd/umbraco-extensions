using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class ZakatCalculatorFieldValidator : ContentValidator {
    private static readonly string AliasPropertyAlias = AliasHelper<ZakatCalculatorFieldSettingsContent>.PropertyAlias(x => x.Alias);
    private static readonly string MetalPropertyAlias = AliasHelper<ZakatCalculatorFieldSettingsContent>.PropertyAlias(x => x.Metal);
    private static readonly string TypePropertyAlias = AliasHelper<ZakatCalculatorFieldSettingsContent>.PropertyAlias(x => x.Type);
    
    public ZakatCalculatorFieldValidator(IContentHelper contentHelper) : base(contentHelper) { }
    
    public override bool IsValidator(ContentProperties content) {
        return content.ContentTypeAlias.EqualsInvariant(PlatformsConstants.Zakat.Settings.Calculator.Field.Alias);
    }

    public override void Validate(ContentProperties content) {
        ValidateAlias(content);
        ValidateMetal(content);
    }

    private void ValidateAlias(ContentProperties content) {
        var aliasProperty = content.GetPropertyByAlias(AliasPropertyAlias);

        if (ContentHelper.AnotherSiblingHasSameValue(content, aliasProperty)) {
            ErrorResult(aliasProperty, "must be unique within the section");
        }
    }

    private void ValidateMetal(ContentProperties content) {
        var typeProperty = content.Properties.Single(x => x.Alias.EqualsInvariant(TypePropertyAlias));
        var type = ContentHelper.GetDataListValue<ZakatCalculatorFieldType>(typeProperty);
        var metalProperty = content.Properties.Single(x => x.Alias.EqualsInvariant(MetalPropertyAlias));
        var metal = ContentHelper.GetDataListValue<Metal>(metalProperty);

        if (type == ZakatCalculatorFieldTypes.Metal && !metal.HasValue()) {
            ErrorResult(metalProperty, "Metal must be specified");
        } else if (type == ZakatCalculatorFieldTypes.Money && metal.HasValue()) {
            ErrorResult(metalProperty, "Metal cannot specified");
        }
    }
}