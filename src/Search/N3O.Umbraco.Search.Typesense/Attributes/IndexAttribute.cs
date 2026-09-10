using System;
using Typesense;

namespace N3O.Umbraco.Search.Typesense.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IndexAttribute : Attribute {
    public IndexAttribute(string name, FieldType type, bool required) {
        Name = name;
        Type = type;
        Required = required;
    }

    public string Name { get; }
    public FieldType Type { get; }
    public bool Required { get; }
}
