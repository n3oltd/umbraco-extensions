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
using OpenIddict.Server;
using System;
using System.Linq;
using System.Reflection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Cms.Web.Website.Controllers;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreHandlerFilters;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreHandlers;

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

        if (WebHostEnvironment.IsStaging()) {
            RegisterStaging(builder);
        }

        builder.Services.AddSingleton<IApplicationReadiness>(sp => sp.GetRequiredService<HomepageWarmup>());

        builder.Services.AddOpenApiDocument("DevTools");
        
        builder.Services.Configure<ApiBehaviorOptions>(c => c.SuppressModelStateInvalidFilter = true);
        builder.Services.Configure<UmbracoRenderingDefaultsOptions>(c => c.DefaultControllerType = typeof(PageController));
        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
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

    private void RegisterStaging(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<BackOfficeStagingCredentialsRemover>();

        builder.Services.Configure<UmbracoPipelineOptions>(opt => AddMiddleware<StagingMiddleware>(opt));

        builder.Services.Configure<OpenIddictServerOptions>(opt => {
            // Must run after the form supplies the client ID and before anything reads the Authorization header
            var tokenOrder = ExtractPostRequest<OpenIddictServerEvents.ExtractTokenRequestContext>.Descriptor.Order;
            var revocationOrder =
                ExtractPostRequest<OpenIddictServerEvents.ExtractRevocationRequestContext>.Descriptor.Order;

            opt.Handlers.Add(OpenIddictServerHandlerDescriptor
                             .CreateBuilder<OpenIddictServerEvents.ExtractTokenRequestContext>()
                             .AddFilter<RequireHttpRequest>()
                             .UseSingletonHandler<BackOfficeStagingCredentialsRemover>()
                             .SetOrder(tokenOrder + 1)
                             .Build());

            opt.Handlers.Add(OpenIddictServerHandlerDescriptor
                             .CreateBuilder<OpenIddictServerEvents.ExtractRevocationRequestContext>()
                             .AddFilter<RequireHttpRequest>()
                             .UseSingletonHandler<BackOfficeStagingCredentialsRemover>()
                             .SetOrder(revocationOrder + 1)
                             .Build());
        });
    }

    private void AddMiddleware<T>(UmbracoPipelineOptions opt) where T : class {
        var filter = new UmbracoPipelineFilter(typeof(T).Name);
        filter.PostPipeline = app => app.UseMiddlewareWhenRunning<T>();

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
