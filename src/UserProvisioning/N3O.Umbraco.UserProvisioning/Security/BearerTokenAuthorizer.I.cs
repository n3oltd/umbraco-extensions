using Microsoft.AspNetCore.Http;

namespace N3O.Umbraco.UserProvisioning.Security;

public interface IBearerTokenAuthorizer {
    bool IsAuthorized(HttpContext context);
}
