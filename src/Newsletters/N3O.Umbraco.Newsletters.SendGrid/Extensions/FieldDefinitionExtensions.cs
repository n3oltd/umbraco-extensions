using N3O.Umbraco.Data.Lookups;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Newsletters.SendGrid.Models;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Extensions;
using DataLookups = N3O.Umbraco.Data.Lookups;

namespace N3O.Umbraco.Newsletters.SendGrid.Extensions;

public static class FieldDefinitionExtensions {
    public static FieldDefinition ToFieldDefinition(this ApiFieldDefinition sgField, bool reserved) {
        var dataType = GetDataType(sgField.FieldType);
        FieldDefinition field;
            
        if (dataType == null) {
            field = FieldDefinition.ForUnsupported(sgField.Id, sgField.Name, reserved);
        } else {
            var sourceDataTypes = GetSourceDataTypes(dataType);

            field = FieldDefinition.ForSupported(sgField.Id, sgField.Name, reserved, dataType, sourceDataTypes);
        }

        return field;
    }
    
    private static  DataType GetDataType(string type) {
        switch (type?.ToLowerInvariant()) {
            case "date":
                return DataLookups.DataTypes.Date;
            
            case "number":
                return DataLookups.DataTypes.Decimal;
            
            case "text":
                return DataLookups.DataTypes.String;

            default:
                return null;
        }
    }
    
    private static IReadOnlyList<DataType> GetSourceDataTypes(DataType dataType) {
        if (dataType == DataLookups.DataTypes.String) {
            return StaticLookups.GetAll<DataType>();
        }

        return dataType.Yield().ToList();
    }
}