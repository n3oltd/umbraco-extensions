using System;
using System.Net;

namespace N3O.Umbraco.UserProvisioning.Exceptions;

public class ScimException : Exception {
    public ScimException(HttpStatusCode status, string detail, string scimType = null) : base(detail) {
        ScimType = scimType;
        Status = status;
    }

    public string ScimType { get; }
    public HttpStatusCode Status { get; }

    public static ScimException Conflict(string detail) {
        return new ScimException(HttpStatusCode.Conflict, detail, ScimConstants.ScimTypes.Uniqueness);
    }

    public static ScimException InvalidFilter(string detail) {
        return new ScimException(HttpStatusCode.BadRequest, detail, ScimConstants.ScimTypes.InvalidFilter);
    }

    public static ScimException InvalidPath(string detail) {
        return new ScimException(HttpStatusCode.BadRequest, detail, ScimConstants.ScimTypes.InvalidPath);
    }

    public static ScimException InvalidValue(string detail) {
        return new ScimException(HttpStatusCode.BadRequest, detail, ScimConstants.ScimTypes.InvalidValue);
    }

    public static ScimException NotFound(string detail) {
        return new ScimException(HttpStatusCode.NotFound, detail);
    }
}
