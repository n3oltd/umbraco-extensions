using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.DataTypes;

public class DataTypesComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IDataTypeEditor, DataTypeEditor>();

        RegisterAll(t => t.ImplementsInterface<IDataTypeDesigner>(),
                    t => builder.Services.AddTransient(t));
    }
}
