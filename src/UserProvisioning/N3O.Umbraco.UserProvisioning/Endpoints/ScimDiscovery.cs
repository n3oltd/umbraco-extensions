using N3O.Umbraco.UserProvisioning.Scim;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Endpoints;

public static class ScimDiscovery {
    public static object ResourceTypes(string baseRoute) {
        var resources = new List<object> {
            ResourceType("User", "Users", ScimConstants.Schemas.User, baseRoute),
            ResourceType("Group", "Groups", ScimConstants.Schemas.Group, baseRoute)
        };

        return new {
            schemas = new[] { ScimConstants.Schemas.ListResponse },
            totalResults = resources.Count,
            itemsPerPage = resources.Count,
            startIndex = 1,
            Resources = resources
        };
    }

    public static object Schemas() {
        var schemas = new List<object> { UserSchema(), GroupSchema() };

        return new {
            schemas = new[] { ScimConstants.Schemas.ListResponse },
            totalResults = schemas.Count,
            itemsPerPage = schemas.Count,
            startIndex = 1,
            Resources = schemas
        };
    }

    public static object ServiceProviderConfig() {
        return new {
            schemas = new[] { ScimConstants.Schemas.ServiceProviderConfig },
            patch = new { supported = true },
            bulk = new { supported = false, maxOperations = 0, maxPayloadSize = 0 },
            filter = new { supported = true, maxResults = 200 },
            changePassword = new { supported = false },
            sort = new { supported = false },
            etag = new { supported = false },
            authenticationSchemes = new[] {
                new {
                    type = "oauthbearertoken",
                    name = "OAuth Bearer Token",
                    description = "Authentication using the Authorization header with a bearer token",
                    primary = true
                }
            }
        };
    }

    private static object Attribute(string name, string type, bool multiValued, bool required, string mutability) {
        return new {
            name,
            type,
            multiValued,
            required,
            caseExact = false,
            mutability,
            returned = "default",
            uniqueness = "none"
        };
    }

    private static object GroupSchema() {
        return new {
            id = ScimConstants.Schemas.Group,
            name = "Group",
            description = "Group",
            attributes = new[] {
                Attribute("displayName", "string", false, true, "readWrite"),
                Attribute("externalId", "string", false, false, "readWrite"),
                Attribute("members", "complex", true, false, "readWrite")
            }
        };
    }

    private static object ResourceType(string name, string endpoint, string schema, string baseRoute) {
        return new {
            schemas = new[] { ScimConstants.Schemas.ResourceType },
            id = name,
            name,
            endpoint = $"/{endpoint}",
            description = name,
            schema,
            meta = new { resourceType = "ResourceType", location = $"{baseRoute}/ResourceTypes/{name}" }
        };
    }

    private static object UserSchema() {
        return new {
            id = ScimConstants.Schemas.User,
            name = "User",
            description = "User",
            attributes = new[] {
                Attribute("userName", "string", false, true, "readWrite"),
                Attribute("displayName", "string", false, false, "readWrite"),
                Attribute("externalId", "string", false, false, "readWrite"),
                Attribute("active", "boolean", false, false, "readWrite"),
                Attribute("name", "complex", false, false, "readWrite"),
                Attribute("emails", "complex", true, false, "readWrite")
            }
        };
    }
}
