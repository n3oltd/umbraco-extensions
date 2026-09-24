using N3O.Umbraco.Extensions;
using System.Text;

namespace N3O.Umbraco.Utilities;

public static class CacheKey {
    private const char NullMarker = '-';

    public static string Generate<T>(params object[] values) {
        var key = new StringBuilder(typeof(T).Name.ToLowerInvariant());

        foreach (var value in values.OrEmpty()) {
            key.Append('|');

            if (value == null) {
                key.Append(NullMarker);
            } else {
                var text = (value.ToString() ?? "").ToLowerInvariant();

                key.Append(text.Length).Append(':').Append(text);
            }
        }

        return key.ToString();
    }
}
