using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace N3O.Umbraco.Bundling.Middleware;

public class SourceMapsStartupFilter : IStartupFilter {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
        return app => {
            app.UseMiddleware<SourceMapsMiddleware>();

            next(app);
        };
    }
}
