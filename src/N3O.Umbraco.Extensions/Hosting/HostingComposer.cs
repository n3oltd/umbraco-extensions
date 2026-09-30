using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Linq;
using System.Reflection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Cms.Web.Website.Controllers;

namespace N3O.Umbraco.Hosting;

public class HostingComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {        
        builder.Services.AddTransient<IConfigureOptions<MvcOptions>, OurMvcBinderOptions>();
        builder.Services.AddTransient<IConfigureOptions<MvcOptions>, OurCacheProfileOptions>();
        builder.Services.AddTransient<IConfigureOptions<RequestLocalizationOptions>, OurRequestLocalizationOptions>();
        builder.Services.AddScoped<IActionLinkGenerator, ActionLinkGenerator>();
        builder.Services.AddSingleton<StaticFileCachePolicy>();
        
        builder.Services.AddScoped<CookiesMiddleware>();
        builder.Services.AddScoped<NotFoundCacheControlMiddleware>();
        builder.Services.AddScoped<StagingMiddleware>();
        builder.Services.AddScoped<WellKnownFolderMiddleware>();

        RegisterHealthChecks(builder);
        RegisterHostedServices(builder);

        builder.Services.AddSingleton<IApplicationReadiness>(sp => sp.GetRequiredService<HomepageWarmup>());

        builder.Services.AddOpenApiDocument("DevTools");
        
        builder.Services.Configure<ApiBehaviorOptions>(c => c.SuppressModelStateInvalidFilter = true);
        builder.Services.Configure<UmbracoRenderingDefaultsOptions>(c => c.DefaultControllerType = typeof(PageController));
        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            if (WebHostEnvironment.IsStaging()) {
                AddMiddleware<StagingMiddleware>(opt);
            }

            AddMiddleware<CookiesMiddleware>(opt);
            AddMiddleware<WellKnownFolderMiddleware>(opt);
            
            ConfigureCors(opt);
        });
        
        builder.Services.Configure<MvcOptions>(options => {
            options.Conventions.Add(new HttpStatusCodesConvention());
        });

        RegisterRouteConstraints(builder);
    }
    
    private void RegisterHealthChecks(IUmbracoBuilder builder) {
        var healthChecksBuilder = builder.Services.AddHealthChecks();

        var healthCheckTypes = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                           t.ImplementsInterface<IHealthCheck>())
                                            .ToList();

        foreach (var type in healthCheckTypes) {
            var attribute = type.GetCustomAttribute<HealthCheckAttribute>();

            if (attribute == null) {
                throw new Exception($"Health check {type.FullName} must have a {nameof(HealthCheckAttribute)} attribute");
            }

            builder.Services.AddTransient(type);

            healthChecksBuilder.Add(new HealthCheckRegistration(attribute.Name,
                                                                sp => (IHealthCheck) sp.GetRequiredService(type),
                                                                failureStatus: null,
                                                                tags: attribute.Tags));
        }
    }

    private void RegisterHostedServices(IUmbracoBuilder builder) {
        var hostedServiceTypes = OurAssemblies.GetTypes(t => t.IsConcreteClass() &&
                                                             t.ImplementsInterface<IHostedService>())
                                              .ToList();

        foreach (var type in hostedServiceTypes) {
            builder.Services.AddSingleton(type);
            builder.Services.AddSingleton(typeof(IHostedService), sp => sp.GetRequiredService(type));
        }
    }

    private void AddMiddleware<T>(UmbracoPipelineOptions opt) where T : class {
        var filter = new UmbracoPipelineFilter(typeof(T).Name);
        filter.PostPipeline = app => {
            var runtimeState = app.ApplicationServices.GetRequiredService<IRuntimeState>();

            if (runtimeState.Level == RuntimeLevel.Run) {
                app.UseMiddleware<T>();
            }
        };

        opt.AddFilter(filter);
    }
    
    private void ConfigureCors(UmbracoPipelineOptions opt) {
        var filter = new UmbracoPipelineFilter("CORS");
        filter.PostPipeline = app => {
            app.UseCors(policy => policy.AllowAnyHeader()
                                        .AllowAnyOrigin()
                                        .AllowAnyMethod()
                                        .SetPreflightMaxAge(TimeSpan.FromMinutes(60)));
        };

        opt.AddFilter(filter);
    }
    
    private void RegisterRouteConstraints(IUmbracoBuilder builder) {
        builder.Services.Configure<RouteOptions>(routeOptions => {
            routeOptions.ConstraintMap.Add("entityId", typeof(EntityIdRouteConstraint));
            routeOptions.ConstraintMap.Add("revisionId", typeof(RevisionIdRouteConstraint));
        });
    }
}
