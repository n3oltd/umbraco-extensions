using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Extensions;

public static class ApplicationBuilderExtensions {
    public static IApplicationBuilder UseMiddlewareWhenRunning<T>(this IApplicationBuilder app) {
        var runtimeState = app.ApplicationServices.GetRequiredService<IRuntimeState>();

        return app.UseWhen(_ => runtimeState.Level == RuntimeLevel.Run, branch => branch.UseMiddleware<T>());
    }


    public static IApplicationBuilder UseOpenApiWithUI(this IApplicationBuilder app) {
        if (OpenApi.IsEnabled()) {
            app.UseOpenApi();
            
            app.UseSwaggerUi(opt => {
                opt.DocExpansion = "list";
                opt.OperationsSorter = "alpha";
                opt.TagsSorter = "alpha";
            });
        }

        return app;
    }
}
