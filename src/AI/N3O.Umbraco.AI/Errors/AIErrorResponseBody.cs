using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Extensions;

namespace N3O.Umbraco.AI.Errors;

public static class AIErrorResponseBody {
    public static string GetErrorCode(string body) {
        return GetErrorProperty(body, "code");
    }

    public static string GetErrorDetailsCode(string body) {
        return GetErrorProperty(body, "details.error_code");
    }

    public static string GetErrorMessage(string body) {
        return GetErrorProperty(body, "message");
    }

    public static string GetErrorType(string body) {
        return GetErrorProperty(body, "type");
    }

    private static string GetErrorProperty(string body, string propertyName) {
        if (body.HasValue() && body.DetectIsJson()) {
            try {
                var token = JObject.Parse(body).SelectToken($"error.{propertyName}");

                if (token?.Type == JTokenType.String) {
                    return token.Value<string>();
                } else {
                    return null;
                }
            } catch (JsonException) {
                return null;
            }
        } else {
            return null;
        }
    }
}
