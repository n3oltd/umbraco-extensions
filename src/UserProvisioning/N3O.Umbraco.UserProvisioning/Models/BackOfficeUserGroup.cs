using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class BackOfficeUserGroup {
    public string Alias { get; set; }
    public string DisplayName { get; set; }
    public string ExternalId { get; set; }
    public string Id { get; set; }
    public IReadOnlyList<BackOfficeUser> Members { get; set; }
}
