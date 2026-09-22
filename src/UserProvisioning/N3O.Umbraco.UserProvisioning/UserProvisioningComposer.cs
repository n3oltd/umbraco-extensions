using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.UserProvisioning.Security;
using N3O.Umbraco.UserProvisioning.Stores;
using Rsk.AspNetCore.Scim.Configuration;
using Rsk.AspNetCore.Scim.Constants;
using System;
using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using ScimGroup = Rsk.AspNetCore.Scim.Models.Group;
using ScimUser = Rsk.AspNetCore.Scim.Models.User;

namespace N3O.Umbraco.UserProvisioning;

public class UserProvisioningComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) {
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
        configOptions.FilteringSupported = true;
        configOptions.PaginationOptions = new PaginationOptions(true, true, PaginationMethod.Index);
        configOptions.PatchSupported = true;
        configOptions.SortingSupported = true;

        builder.Services
               .AddScimServiceProvider(settings.BaseRoute, licensingOptions, configOptions)
               .AddResource<ScimUser, UserStore>(ScimSchemas.User, UserProvisioningConstants.Resources.Users)
               .AddResource<ScimGroup, UserGroupStore>(ScimSchemas.Group, UserProvisioningConstants.Resources.Groups)
               .AddFilterPropertyExpressionCompiler()
               .MapScimAttributes<BackOfficeUser>(ScimSchemas.User,
                                                  mapper => mapper.Map("id", x => x.Id)
                                                                  .Map("userName", x => x.UserName)
                                                                  .Map("externalId", x => x.UserName)
                                                                  .Map("active", x => x.Active)
                                                                  .Map("displayName", x => x.Name))
               .MapScimAttributes<BackOfficeUserGroup>(ScimSchemas.Group,
                                                       mapper => mapper.Map("id", x => x.Id)
                                                                       .Map("displayName", x => x.DisplayName))
               .AddScimAuthorization<BearerTokenAuthorizer>();

        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(UserProvisioningConstants.PipelineFilterName);
            filter.PrePipeline = app => app.UseScim();

            opt.AddFilter(filter);
        });
    }

    private void Validate(UserProvisioningSettings settings) {
        if (!settings.BearerToken.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no BearerToken");
        }

        if (!settings.LicenseKey.HasValue() || !settings.Licensee.HasValue()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but has no licence");
        }

        if (!settings.UserGroups.Any()) {
            throw new Exception($"{UserProvisioningSettings.SectionName} is enabled but maps no user groups");
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
