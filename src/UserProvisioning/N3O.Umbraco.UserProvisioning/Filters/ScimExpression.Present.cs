namespace N3O.Umbraco.UserProvisioning.Filters;

public class PresentScimExpression : ScimExpression {
    public PresentScimExpression(ScimAttributePath path) {
        Path = path;
    }

    public ScimAttributePath Path { get; }

    public override bool Matches(ScimAttributes attributes) {
        return attributes.Has(Path);
    }

    public override string ToString() {
        return $"{Path} pr";
    }
}
