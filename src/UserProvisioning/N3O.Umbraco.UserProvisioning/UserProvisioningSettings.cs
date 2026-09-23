using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Extensions;
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
    public string EditorGroups { get; set; }
    public bool Enabled { get; set; }
    public string GovernedDomains { get; set; }
    public bool LogRequests { get; set; }

    public IReadOnlyList<UserProvisioningGroup> UserGroups => Named(AdministratorGroups,
                                                                   UmbracoConstants.Security.AdminGroupAlias)
                                                            .Concat(Named(EditorGroups,
                                                                          UmbracoConstants.Security.EditorGroupAlias))
                                                            .ToList();

    // An address the directory cannot own is somebody the directory must not manage, whichever user
    // group holds them
    public bool Governs(string email) {
        if (!email.HasValue()) {
            return false;
        }

        var at = email.LastIndexOf('@');

        return at >= 0 && Split(GovernedDomains).Any(x => x.Is(email.Substring(at + 1)));
    }

    private static IEnumerable<UserProvisioningGroup> Named(string groups, string alias) {
        return Split(groups).Select(x => new UserProvisioningGroup(x, alias));
    }

    private static IEnumerable<string> Split(string value) {
        if (!value.HasValue()) {
            return Enumerable.Empty<string>();
        }

        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
