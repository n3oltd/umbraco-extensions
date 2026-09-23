namespace N3O.Umbraco.UserProvisioning.Filters;

public class ScimToken {
    public ScimToken(ScimTokenType type, string text) {
        Text = text;
        Type = type;
    }

    public string Text { get; }
    public ScimTokenType Type { get; }

    public override string ToString() {
        return $"{Type}:{Text}";
    }
}
