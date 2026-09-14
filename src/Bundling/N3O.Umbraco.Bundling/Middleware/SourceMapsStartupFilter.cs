using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace N3O.Umbraco.Bundling.Middleware;

// CmsStartup calls UseStaticFiles before UseUmbraco, so Umbraco's own pipeline hooks all run too late to
// withhold a .map. An IStartupFilter wraps the host's Configure, which is the only seam early enough.
public class SourceMapsStartupFilter : IStartupFilter {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
        return app => {
            app.UseMiddleware<SourceMapsMiddleware>();

            next(app);
        };
    }
}
