using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning;

public class UserProvisioningSettings {
    public const string SectionName = "N3O:UserProvisioning";

    public string BaseRoute { get; set; } = "/umbraco/scim";
    public string BearerToken { get; set; }
    public string DefaultUserGroupAlias { get; set; } = "editor";
    public bool Enabled { get; set; }
    public bool LogRequests { get; set; }
    public Dictionary<string, string> UserGroups { get; set; } = new();
}
