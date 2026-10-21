using N3O.Umbraco.AI.Errors;
using System.ClientModel;

namespace N3O.Umbraco.AI.Extensions;

public static class ClientResultExceptionExtensions {
    public static string GetErrorCode(this ClientResultException ex) {
        return AIErrorResponseBody.GetErrorCode(GetResponseBody(ex));
    }

    public static string GetErrorType(this ClientResultException ex) {
        return AIErrorResponseBody.GetErrorType(GetResponseBody(ex));
    }

    private static string GetResponseBody(ClientResultException ex) {
        return ex.GetRawResponse()?.Content.ToString();
    }
}
