using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace N3O.Umbraco.ValueConverters;

public class ValueConvertersComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.PropertyValueConverters().InsertBefore<MultiNodeTreePickerValueConverter, StronglyTypedMultiNodeTreePickerValueConverter>();
    }
}
