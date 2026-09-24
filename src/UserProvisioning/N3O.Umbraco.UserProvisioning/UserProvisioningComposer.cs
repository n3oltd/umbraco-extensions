using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Hosting;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Services;
using System;
using System.Linq;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace N3O.Umbraco.UserProvisioning;

public class UserProvisioningComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        var settings = builder.Config
                              .GetSection(UserProvisioningSettings.SectionName)
                              .Get<UserProvisioningSettings>();

        if (settings == null || !settings.Enabled) {
            return;
        }

        Validate(settings);

        builder.Services.AddSingleton(settings);
        builder.Services.AddScoped<IBearerTokenAuthorizer, BearerTokenAuthorizer>();
        builder.Services.AddScoped<ScimMiddleware>();
        builder.Services.AddScoped<IScimState, ScimState>();
        builder.Services.AddScoped<IScimStore<ScimGroup>, UserGroupStore>();
        builder.Services.AddScoped<IScimStore<ScimUser>, UserStore>();

        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(UserProvisioningConstants.PipelineFilterName);
            filter.PrePipeline = app => app.UseMiddleware<ScimMiddleware>();

            opt.AddFilter(filter);
        });
    }

    private void Validate(UserProvisioningSettings settings) {
        if (!settings.BaseRoute.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no BaseRoute");
        }

        // PathString throws on a route with no leading slash and matches every path when it is empty, and
        // the match runs ahead of the handler that would turn either into a refusal
        if (!settings.BaseRoute.StartsWith('/') || !settings.BaseRoute.Trim('/').HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} BaseRoute " +
                                $"{settings.BaseRoute.Quote()} must begin with a slash and name a path");
        }

        if (!settings.BearerToken.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no BearerToken");
        }

        if (!settings.GovernedDomains.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but names no " +
                                $"GovernedDomains");
        }

        if (!settings.UserGroups.Any()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but names no groups in " +
                                $"AdministratorGroups or EditorGroups");
        }

        var duplicated = settings.UserGroups
                                 .GroupBy(x => x.DisplayName, ScimText.Comparer)
                                 .Where(x => x.Count() > 1)
                                 .Select(x => x.Key)
                                 .ToList();

        if (duplicated.Any()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} names " +
                                $"{string.Join(", ", duplicated.Select(x => x.Quote()))} in more than one role");
        }
    }
}
