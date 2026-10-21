using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;
using Umbraco.Cms.Core.DependencyInjection;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.Publisher;
using uSyncCoreConstants = uSync.Core.uSyncConstants;

namespace N3O.Umbraco.Sync;

public class SyncComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.AdduSync();

        builder.Services.PostConfigure<uSyncHandlerSetSettings>(uSyncPublisher.PublisherHandlerSet,
                                                                IncludeSharedValues);

        builder.Services.Configure<MvcOptions>(options => options.Filters.Add<CaptureSyncCultures>());
    }

    private static void IncludeSharedValues(uSyncHandlerSetSettings handlerSet) {
        IncludeSharedValues(handlerSet.HandlerDefaults);

        foreach (var settings in handlerSet.Handlers.OrEmpty(x => x.Values)) {
            IncludeSharedValues(settings);
        }
    }

    private static void IncludeSharedValues(HandlerSettings settings) {
        if (settings?.Settings == null) {
            return;
        }

        var configuredKeys = settings.Settings
                                     .Keys
                                     .Where(x => string.Equals(x,
                                                               uSyncCoreConstants.DefaultsKey,
                                                               StringComparison.OrdinalIgnoreCase))
                                     .ToList();

        var includeSharedValues = true;

        foreach (var key in configuredKeys) {
            if (bool.TryParse(settings.Settings[key], out var configuredValue)) {
                includeSharedValues = configuredValue;
            }

            settings.Settings.Remove(key);
        }

        settings.Settings[uSyncCoreConstants.DefaultsKey] = includeSharedValues.ToString();
    }
}
