namespace N3O.Umbraco.Search.Typesense.Queries;

public class TypesenseParameters {
    public TypesenseParameters(string name, string value) {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}
