using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ClientModel;
using Umbraco.Extensions;

namespace N3O.Umbraco.AI.Extensions;

public static class ClientResultExceptionExtensions {
    public static string GetErrorType(this ClientResultException ex) {
        var content = ex.GetRawResponse()?.Content.ToString();

        if (!content.HasValue() || !content.DetectIsJson()) {
            return null;
        }

        try {
            return JObject.Parse(content).SelectToken("$.error.type")?.Value<string>();
        } catch (JsonException) {
            return null;
        }
    }
}
