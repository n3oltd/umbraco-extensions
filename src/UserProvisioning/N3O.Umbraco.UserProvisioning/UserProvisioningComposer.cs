using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Endpoints;
using N3O.Umbraco.UserProvisioning.Scim;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Stores;
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

        if (!settings.UserGroups.Any()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but maps no user groups");
        }

        if (!settings.UserGroups.Values.Any(x => x.EqualsInvariant(settings.DefaultUserGroupAlias))) {
            throw new Exception($"{UserProvisioningSettings.SectionName} default user group " +
                                $"{settings.DefaultUserGroupAlias.Quote()} is not one of the mapped user groups");
        }

        var duplicated = settings.UserGroups
                                 .GroupBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
                                 .Where(x => x.Count() > 1)
                                 .Select(x => x.Key)
                                 .ToList();

        if (duplicated.Any()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} maps more than one group to " +
                                $"{string.Join(", ", duplicated.Select(x => x.Quote()))}");
        }
    }
}
