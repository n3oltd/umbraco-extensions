using Microsoft.AspNetCore.Html;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Extensions;

public static class StringExtensions {
    public static int CompareInvariant(this string a, string b) {
        return string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase);
    }
    
    public static string CombineWith(this string string1, string string2, string separator = ".") {
        if (!string1.HasValue() && !string2.HasValue()) {
            return null;
        }

        var combined = "";

        if (string1.HasValue()) {
            combined += string1;

            if (string2.HasValue()) {
                combined += separator;
            }
        }

        if (string2.HasValue()) {
            combined += string2;
        }

        return combined;
    }

    public static string DigitsOnly(this string s, char? decimalSeparator = null) {
        if (s == null) {
            return null;
        }

        return Regex.Replace(s, $"[^0-9{decimalSeparator}]", "");
    }
    
    public static string EnsureTrailingSlash(this string s) {
        if (string.IsNullOrEmpty(s)) {
            return string.Empty;
        }
        
        return Regex.Replace(s, "/+$", string.Empty) + "/";
    }

    public static bool EqualsInvariant(this string a, string b) {
        return CompareInvariant(a, b) == 0;
    }

    public static string FormatWith(this string s, params object[] args) {
        return string.Format(s, args);
    }

    // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
    public static int GetDeterministicHashCode(this string str, bool caseAndCultureInsensitive) {
        if (caseAndCultureInsensitive) {
            str = str.ToLowerInvariant();
        }

        unchecked {
            var hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length; i += 2) {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];

                if (i == str.Length - 1) {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    public static string GetFileExtension(this string pathOrUrl) {
        var str = pathOrUrl;

        if (str.Contains("?")) {
            str = str.Substring(0, str.IndexOf('?'));
        }

        var index = str.LastIndexOf('.');

        if (index == -1) {
            return null;
        }

        return str.Substring(index).ToLowerInvariant();
    }

    public static bool HasValue(this string s) {
        return !IsNullOrWhiteSpace(s);
    }

    public static bool IsNullOrEmpty(this string s) {
        return string.IsNullOrEmpty(s);
    }

    public static bool IsNullOrWhiteSpace(this string s) {
        return string.IsNullOrWhiteSpace(s);
    }
    
    public static bool IsOnDomain(this string url, params string[] domains) {
        var absoluteUrl = url.ToAbsoluteUrl();

        if (absoluteUrl == null) {
            return false;
        }

        var host = new Uri(absoluteUrl).Host.TrimEnd('.');

        return domains.Any(x => host.EqualsInvariant(x) ||
                                host.EndsWith($".{x}", StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsValidEmailAddress(this string email) {
        if (!email.HasValue()) {
            return false;
        }

        var index = email.IndexOf('@');

        return index > 0 &&
               index != email.Length - 1 &&
               index == email.LastIndexOf('@');
    }
    
    public static bool IsValidUrl(this string url, params string[] uriSchemes) {
        var allowedSchemes = uriSchemes.Or([Uri.UriSchemeHttp, Uri.UriSchemeHttps]);

        var isValid = Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                      allowedSchemes.Contains(uriResult.Scheme);

        return isValid;
    }
    
    public static string Left(this string input, int count) {
        return input.Substring(0, Math.Min(input.Length, count));
    }

    public static string Or(this string a, string b) {
        return a.HasValue() ? a : b;
    }

    public static string Quote(this string s) {
        if (!s.HasValue()) {
            return null;
        }

        return $"'{s}'";
    }

    public static string RemoveDiacritics(this string s) {
        var normalized = s.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalized) {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
        
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string RemoveLeading(this string s, string toRemove) {
        while (s?.StartsWith(toRemove) == true) {
            s = s.Substring(toRemove.Length);
        }

        return s;
    }

    public static string RemoveLeadingSlashes(this string s) {
        return RemoveLeading(s, "/");
    }

    public static string RemoveLeadingZeros(this string s) {
        return RemoveLeading(s, "0");
    }

    public static string RemoveNonAscii(this string s) {
        if (s == null) {
            return null;
        }

        s = s.RemoveDiacritics();

        return Regex.Replace(s, @"[^\u0000-\u007F]+", string.Empty);
    }

    public static string RemoveWhitespace(this string s) {
        if (s == null) {
            return null;
        }

        return Regex.Replace(s, @"\s", "");
    }

    public static string Repeat(this string s, int count) {
        if (s.IsNullOrEmpty()) {
            return s;
        }
    
        if (count == 0) {
            return "";
        }

        return string.Concat(Enumerable.Repeat(s, count));
    }
    
    public static string Right(this string s, int tailLength) {
        if (tailLength >= s.Length) {
            return s;
        }

        return s.Substring(s.Length - tailLength);
    }

    public static string Sha1(this string s) {
        using (var sha1 = SHA1.Create()) {
            var bytes = Encoding.UTF8.GetBytes(s);
            var hash = sha1.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);

            foreach (var b in hash) {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
    
    public static string Sha256(this string s) {
        using (var sha256 = SHA256.Create()) {
            var bytes = Encoding.UTF8.GetBytes(s);
            var hash = sha256.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);

            foreach (var b in hash) {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
    
    public static string StripTrailingSlash(this string s) {
        if (string.IsNullOrEmpty(s)) {
            return string.Empty;
        }
        
        return Regex.Replace(s, "/+$", string.Empty);
    }
    
    public static string ToAbsoluteUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var trimmedUrl = url.Trim();

        if (trimmedUrl.IsValidUrl()) {
            return trimmedUrl;
        } else if (trimmedUrl.StartsWith("//") && $"https:{trimmedUrl}".IsValidUrl()) {
            return $"https:{trimmedUrl}";
        } else if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out _)) {
            return null;
        } else if ($"https://{trimmedUrl}".IsValidUrl()) {
            return $"https://{trimmedUrl}";
        } else {
            return null;
        }
    }

    public static TEnum? ToEnum<TEnum>(this string str) where TEnum : struct {
        if (str.HasValue() && Enum.TryParse<TEnum>(str, true, out var parseResult)) {
            return parseResult;
        } else {
            return null;
        }
    }
    
    public static HtmlString ToHtmlString(this string s) {
        if (s == null) {
            return null;
        }
    
        return new HtmlString(s);
    }

    public static string TrimOrNull(this string s) {
        s = s?.Trim();

        return s.HasValue() ? s : null;
    }

    public static T? TryParseAs<T>(this string s) where T : struct {
        if (!s.HasValue()) {
            return null;
        }

        object value = null;

        try {
            if (typeof(T) == typeof(int)) {
                value = int.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
            } else if (typeof(T) == typeof(double)) {
                value = double.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
            } else if (typeof(T) == typeof(decimal)) {
                value = decimal.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
            } else if (typeof(T) == typeof(bool)) {
                value = bool.Parse(s);
            } else if (typeof(T) == typeof(DateTime)) {
                value = DateTime.Parse(s);
            } else if (typeof(T) == typeof(TimeSpan)) {
                value = TimeSpan.Parse(s);
            } else if (typeof(T) == typeof(Guid)) {
                value = Guid.Parse(s);
            } else if (typeof(T).IsEnum) {
                value = Enum.Parse(typeof(T), s, true);
            } else {
                value = (T) Convert.ChangeType(s, typeof(T));
            }
        } catch { }

        return (T?) value;
    }

    public static string When(this string s, bool condition) {
        return condition ? s : null;
    }
}
