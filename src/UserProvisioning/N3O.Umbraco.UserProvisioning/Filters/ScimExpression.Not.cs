namespace N3O.Umbraco.UserProvisioning.Filters;

public class NotScimExpression : ScimExpression {
    public NotScimExpression(ScimExpression inner) {
        Inner = inner;
    }

    public ScimExpression Inner { get; }

    public override bool Matches(ScimAttributes attributes) {
        return !Inner.Matches(attributes);
    }

    public override string ToString() {
        return $"not ({Inner})";
    }
}
