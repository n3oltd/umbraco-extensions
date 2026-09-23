namespace N3O.Umbraco.UserProvisioning.Filters;

public abstract class ScimExpression {
    public abstract bool Matches(ScimAttributes attributes);
}
