using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Services;

public static class ScimPatch {
    public static void Validate(IEnumerable<ScimPatchOperation> operations) {
        foreach (var operation in operations.OrEmpty()) {
            if (operation == null) {
                throw ScimException.InvalidValue("A patch operation cannot be null");
            }

            var op = operation.Op ?? "";

            if (!op.Is("add") && !op.Is("remove") && !op.Is("replace")) {
                throw ScimException.InvalidValue($"{operation.Op.Quote()} is not a patch operation");
            }

            if (op.Is("remove") && ScimPath.Parse(operation.Path) == null) {
                throw ScimException.NoTarget("A removal must name its target with a path");
            }
        }
    }
}
