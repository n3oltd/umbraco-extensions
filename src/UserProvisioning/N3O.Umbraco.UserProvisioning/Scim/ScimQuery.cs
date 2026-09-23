using N3O.Umbraco.UserProvisioning.Filters;

namespace N3O.Umbraco.UserProvisioning.Scim;

public class ScimQuery {
    public int Count { get; set; } = 100;
    public ScimExpression Filter { get; set; }
    public int StartIndex { get; set; } = 1;
}
