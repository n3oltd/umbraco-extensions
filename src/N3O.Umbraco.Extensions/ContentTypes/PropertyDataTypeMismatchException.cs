using N3O.Umbraco.Extensions;
using System;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.ContentTypes;

public class PropertyDataTypeMismatchException : Exception {
    public PropertyDataTypeMismatchException(string alias,
                                             string propertyAlias,
                                             ValueStorageType currentStorageType,
                                             ValueStorageType storageType)
        : base($"Property {propertyAlias.Quote()} on content type {alias.Quote()} stores its values as " +
               $"{currentStorageType}, and the data type it would be bound to stores them as {storageType}, " +
               $"so rebinding it would leave the values it already holds unreadable") {
        Alias = alias;
        PropertyAlias = propertyAlias;
        CurrentStorageType = currentStorageType;
        StorageType = storageType;
    }

    public string Alias { get; }
    public string PropertyAlias { get; }
    public ValueStorageType CurrentStorageType { get; }
    public ValueStorageType StorageType { get; }
}
