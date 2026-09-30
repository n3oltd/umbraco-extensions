using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using N3O.Umbraco.Composing;
using System;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Utilities;

public class UtilitiesComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
        builder.Services.TryAddSingleton<IUrlBuilder, UrlBuilder>();
    }
}
