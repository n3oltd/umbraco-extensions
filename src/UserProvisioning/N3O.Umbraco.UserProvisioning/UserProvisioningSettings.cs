using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning;

public class UserProvisioningSettings {
    public const string SectionName = "N3O:UserProvisioning";

    public string AdministratorGroups { get; set; }
    public string BaseRoute { get; set; } = "/umbraco/scim";
    public string BearerToken { get; set; }
    public bool Enabled { get; set; }
    public bool LogRequests { get; set; }
    public string EditorGroups { get; set; }

    // Which Umbraco group a role provisions into is fixed by the package, so a site configures only
    // which directory groups fill each role
    public IReadOnlyList<UserProvisioningGroup> UserGroups => Named(AdministratorGroups,
                                                                   UmbracoConstants.Security.AdminGroupAlias)
                                                            .Concat(Named(EditorGroups,
                                                                          UmbracoConstants.Security.EditorGroupAlias))
                                                            .ToList();

    private static IEnumerable<UserProvisioningGroup> Named(string groups, string alias) {
        if (!groups.HasValue()) {
            return Enumerable.Empty<UserProvisioningGroup>();
        }

        return groups.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(x => new UserProvisioningGroup(x, alias));
    }
}
