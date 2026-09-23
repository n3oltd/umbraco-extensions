namespace N3O.Umbraco.UserProvisioning.Filters;

public class OrScimExpression : ScimExpression {
    public OrScimExpression(ScimExpression left, ScimExpression right) {
        Left = left;
        Right = right;
    }

    public ScimExpression Left { get; }
    public ScimExpression Right { get; }

    public override bool Matches(ScimAttributes attributes) {
        return Left.Matches(attributes) || Right.Matches(attributes);
    }

    public override string ToString() {
        return $"({Left} or {Right})";
    }
}
