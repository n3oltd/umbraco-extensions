using System;
using Typesense;

namespace N3O.Umbraco.Search.Typesense.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FieldAttribute : Attribute {
    public FieldAttribute(string name,
                          FieldType type,
                          bool required,
                          bool index,
                          bool facet = false,
                          bool sort = false,
                          bool infix = false,
                          string locale = null,
                          int numberOfDimensions = 0) {
        Name = name;
        Type = type;
        Required = required;
        Index = index;
        Facet = facet;
        Sort = sort;
        Infix = infix;
        Locale = locale;
        NumberOfDimensions = numberOfDimensions;
    }
    
    public string Name { get; }
    public FieldType Type { get; }
    public bool Required { get; }
    public bool Index { get; }
    public bool Facet { get; }
    public bool Sort { get; }
    public bool Infix { get; }
    public string Locale { get; }
    public int NumberOfDimensions { get; }
}
