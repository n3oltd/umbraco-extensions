namespace N3O.Umbraco.UserProvisioning;

public static class ScimConstants {
    public const string ContentType = "application/scim+json";

    public static class Schemas {
        public const string Error = "urn:ietf:params:scim:api:messages:2.0:Error";
        public const string Group = "urn:ietf:params:scim:schemas:core:2.0:Group";
        public const string ListResponse = "urn:ietf:params:scim:api:messages:2.0:ListResponse";
        public const string PatchOp = "urn:ietf:params:scim:api:messages:2.0:PatchOp";
        public const string ResourceType = "urn:ietf:params:scim:schemas:core:2.0:ResourceType";
        public const string Schema = "urn:ietf:params:scim:schemas:core:2.0:Schema";
        public const string ServiceProviderConfig = "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig";
        public const string User = "urn:ietf:params:scim:schemas:core:2.0:User";
    }

    public static class ScimTypes {
        public const string InvalidFilter = "invalidFilter";
        public const string InvalidPath = "invalidPath";
        public const string InvalidSyntax = "invalidSyntax";
        public const string InvalidValue = "invalidValue";
        public const string NoTarget = "noTarget";
        public const string Uniqueness = "uniqueness";
    }
}
