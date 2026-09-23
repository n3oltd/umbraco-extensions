using N3O.Umbraco.UserProvisioning.Models;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Services;

public static class ScimPaging {
    // startIndex is 1-based in SCIM, and a count of zero asks for the total without any resources
    public static ScimListResponse<T> Page<T>(IReadOnlyList<T> matching, ScimQuery query) {
        var skip = query.StartIndex < 1 ? 0 : query.StartIndex - 1;
        var take = query.Count < 0 ? 0 : query.Count;

        var response = new ScimListResponse<T>();
        response.ItemsPerPage = take;
        response.Resources = matching.Skip(skip).Take(take).ToList();
        response.StartIndex = skip + 1;
        response.TotalResults = matching.Count;

        return response;
    }
}
