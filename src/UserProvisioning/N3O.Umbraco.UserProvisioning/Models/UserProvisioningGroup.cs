namespace N3O.Umbraco.UserProvisioning.Models;

public class UserProvisioningGroup {
    public UserProvisioningGroup(string displayName, string alias) {
        Alias = alias;
        DisplayName = displayName;
    }

    public string Alias { get; }
    public string DisplayName { get; }
}
