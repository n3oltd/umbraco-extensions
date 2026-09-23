using N3O.Umbraco.UserProvisioning.Models;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Hosting;

public static class ScimDiscovery {
    public static ScimListResponse<object> ResourceTypes(string baseRoute) {
        return List(new List<object> {
            ResourceType("User", "Users", ScimConstants.Schemas.User, baseRoute),
            ResourceType("Group", "Groups", ScimConstants.Schemas.Group, baseRoute)
        });
    }

    public static ScimListResponse<object> Schemas() {
        return List(new List<object> { UserSchema(), GroupSchema() });
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
            description = name,
            required,
            caseExact = false,
            mutability,
            returned = "default",
            uniqueness = "none"
        };
    }

    private static ScimListResponse<object> List(IReadOnlyList<object> resources) {
        var response = new ScimListResponse<object>();
        response.ItemsPerPage = resources.Count;
        response.Resources = resources;
        response.StartIndex = 1;
        response.TotalResults = resources.Count;

        return response;
    }

    private static object GroupSchema() {
        return new {
            schemas = new[] { ScimConstants.Schemas.Schema },
            id = ScimConstants.Schemas.Group,
            name = "Group",
            description = "Group",
            meta = new { resourceType = "Schema", location = $"/Schemas/{ScimConstants.Schemas.Group}" },
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
            schemas = new[] { ScimConstants.Schemas.Schema },
            id = ScimConstants.Schemas.User,
            name = "User",
            description = "User",
            meta = new { resourceType = "Schema", location = $"/Schemas/{ScimConstants.Schemas.User}" },
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
