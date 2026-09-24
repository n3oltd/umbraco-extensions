using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Dev;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cms;

public abstract class CmsStartup {
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    protected CmsStartup(IWebHostEnvironment webHostEnvironment, IConfiguration configuration) {
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;

        // Default min is ProcessorCount which is too low when sites burst many concurrent async HTTP calls
        // (e.g. at startup); thread pool grows lazily and starves
        ThreadPool.SetMinThreads(100, 100);

        EnvironmentData.LoadFromConfiguration(configuration);
        DevSettings.Apply(webHostEnvironment, configuration);
    }

    public void ConfigureServices(IServiceCollection services) {
        Composer.WebHostEnvironment = _webHostEnvironment;

        services.AddUmbraco(_webHostEnvironment, _configuration)
                .AddBackOffice()
                .AddWebsite()
                .AddDeliveryApi()
                .AddComposers()
                .AddContentment(opt => opt.DisableTelemetry = true)
                .Build();

        services.Configure<StaticFileOptions>(ConfigureStaticFileOptions);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if (env.IsProduction()) {
            app.UseHsts();
        } else {
            app.UseDeveloperExceptionPage();
        }

        app.UseRewriter(GetRewriteOptions());
        
        var staticFileOptions = new StaticFileOptions();
        ConfigureStaticFiles(staticFileOptions);
        
        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/media"),
                    appBuilder => appBuilder.UseStaticFiles());
        
        app.UseOpenApiWithUI();

        app.UseHealthChecks("/healthz", new HealthCheckOptions {
            Predicate = c => c.Tags.Contains(HealthCheckTags.Readiness),
            ResponseWriter = (_, _) => Task.CompletedTask
        });

        app.UseHealthChecks("/livez", new HealthCheckOptions {
            Predicate = c => c.Tags.Contains(HealthCheckTags.Liveness),
            ResponseWriter = (_, _) => Task.CompletedTask
        });

        app.UseUmbraco()
           .WithMiddleware(u => {
               u.UseBackOffice();
               u.UseWebsite();

               ConfigureMiddleware(u);
           })
           .WithEndpoints(u => {
               u.UseInstallerEndpoints();
               u.UseBackOfficeEndpoints();
               u.UseWebsiteEndpoints();

               u.RunExtensions();

               ConfigureEndpoints(u);
           });
    }

    protected virtual void ConfigureEndpoints(IUmbracoEndpointBuilderContext umbraco) { }
    protected virtual void ConfigureMiddleware(IUmbracoApplicationBuilderContext umbraco) { }
    protected virtual void ConfigureStaticFiles(StaticFileOptions staticFileOptions) { }

    private void ConfigureStaticFileOptions(StaticFileOptions staticFileOptions) {
        staticFileOptions.OnPrepareResponse = context => {
            if (!context.Context.Request.Path.StartsWithSegments("/media")) {
                context.Context.Response.Headers.CacheControl = "no-cache";
            }
        };
    }

    private RewriteOptions GetRewriteOptions() {
        var options = new RewriteOptions();
        
        var rules = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                t.HasParameterlessConstructor() &&
                                                t.ImplementsInterface<IRule>())
                                 .ApplyAttributeOrdering()
                                 .Select(t => (IRule) Activator.CreateInstance(t))
                                 .ToList();

        rules.Do(x => options.Rules.Add(x));

        return options;
    }
}
