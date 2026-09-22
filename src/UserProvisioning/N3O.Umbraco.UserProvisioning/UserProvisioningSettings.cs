using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning;

public class UserProvisioningSettings {
    public const string SectionName = "N3O:UserProvisioning";

    public string BaseRoute { get; set; } = "/umbraco/scim";
    public string BearerToken { get; set; }
    public string DefaultUserGroupAlias { get; set; } = "editor";
    public bool Enabled { get; set; }
    public string Licensee { get; set; }
    public string LicenseKey { get; set; }
    public Dictionary<string, string> UserGroups { get; set; } = new();
}
