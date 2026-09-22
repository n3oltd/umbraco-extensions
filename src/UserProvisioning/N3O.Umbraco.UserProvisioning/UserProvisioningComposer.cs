using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Stores;
using Rsk.AspNetCore.Scim.Configuration;
using Rsk.AspNetCore.Scim.Constants;
using Rsk.AspNetCore.Scim.Models;
using System;
using System.Linq;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using ScimGroup = Rsk.AspNetCore.Scim.Models.Group;
using ScimUser = Rsk.AspNetCore.Scim.Models.User;

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
        builder.Services.AddHttpContextAccessor();

        var licensingOptions = new ScimLicensingOptions();
        licensingOptions.Licensee = settings.Licensee;
        licensingOptions.LicenseKey = settings.LicenseKey;

        var configOptions = new ScimServiceProviderConfigOptions();
        configOptions.EnableAzureAdCompatibility = true;
        configOptions.EnableRequestAndResponseLogging = settings.LogRequests;
        configOptions.FilteringSupported = true;
        configOptions.PaginationOptions = new PaginationOptions(true, true, PaginationMethod.Index);
        configOptions.PatchSupported = true;
        configOptions.SortingSupported = true;

        builder.Services
               .AddScimServiceProvider(settings.BaseRoute, licensingOptions, configOptions)
               .AddResource<ScimUser, UserStore>(ScimSchemas.User, UserProvisioningConstants.Resources.Users)
               .AddResource<ScimGroup, UserGroupStore>(ScimSchemas.Group, UserProvisioningConstants.Resources.Groups)
               .AddFilterPropertyExpressionCompiler()
               .MapScimAttributes<BackOfficeUser>(ScimSchemas.User, MapUserProjection)
               .MapScimAttributes<BackOfficeUserGroup>(ScimSchemas.Group, MapUserGroupProjection)
               .MapScimAttributes<ScimUser>(ScimSchemas.User, MapUser)
               .MapScimAttributes<ScimGroup>(ScimSchemas.Group, MapUserGroup)
               .AddScimAuthorization<BearerTokenAuthorizer>();

        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(UserProvisioningConstants.PipelineFilterName);
            filter.PrePipeline = app => app.UseScim();

            opt.AddFilter(filter);
        });
    }

    // The filter compiler binds against the projections the stores query, and the patch executor
    // against the resources they hand it, and the map is keyed on the type rather than the schema
    private static void MapUser(IScimAttributeToPropertyBuilder<ScimUser> mapper) {
        mapper.Map("active", x => x.Active)
              .Map("displayName", x => x.DisplayName)
              .Map("externalId", x => x.ExternalId)
              .Map("id", x => x.Id)
              .Map("userName", x => x.UserName)
              .MapComplex("name", x => x.Name, n => n.Map("familyName", x => x.FamilyName)
                                                     .Map("formatted", x => x.Formatted)
                                                     .Map("givenName", x => x.GivenName))
              .MapCollection<Email>("emails", x => x.Emails, e => e.Map("primary", x => x.Primary)
                                                                   .Map("type", x => x.Type)
                                                                   .Map("value", x => x.Value));
    }

    private static void MapUserGroup(IScimAttributeToPropertyBuilder<ScimGroup> mapper) {
        mapper.Map("displayName", x => x.DisplayName)
              .Map("externalId", x => x.ExternalId)
              .Map("id", x => x.Id)
              .MapCollection<Member>("members", x => x.Members, m => m.Map("display", x => x.Display)
                                                                      .Map("type", x => x.Type)
                                                                      .Map("value", x => x.Value));
    }

    private static void MapUserGroupProjection(IScimAttributeToPropertyBuilder<BackOfficeUserGroup> mapper) {
        mapper.Map("displayName", x => x.DisplayName)
              .Map("id", x => x.Id);
    }

    private static void MapUserProjection(IScimAttributeToPropertyBuilder<BackOfficeUser> mapper) {
        mapper.Map("active", x => x.Active)
              .Map("displayName", x => x.Name)
              .Map("id", x => x.Id)
              .Map("userName", x => x.UserName);
    }

    private void Validate(UserProvisioningSettings settings) {
        if (!settings.BaseRoute.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no BaseRoute");
        }

        if (!settings.BearerToken.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no BearerToken");
        }

        if (!settings.LicenseKey.HasValue() || !settings.Licensee.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no licence");
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
