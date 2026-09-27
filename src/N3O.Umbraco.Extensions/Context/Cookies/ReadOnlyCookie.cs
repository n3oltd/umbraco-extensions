using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using System.Linq;

namespace N3O.Umbraco.Context;

public abstract class ReadOnlyCookie : IReadOnlyCookie {
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    protected ReadOnlyCookie(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string GetValue() {
        if (Value == null) {
            var cookies = _httpContextAccessor.HttpContext?.Request.Cookies;
            var key = cookies?.Keys.SingleOrDefault(x => x.EqualsInvariant(Name));

            if (key != null) {
                Value = cookies[key];

                // What is rendered from a visitor's own cookie is theirs alone.
                EdgeCaching.Prevent(_httpContextAccessor.HttpContext);
            } else {
                Value = GetDefaultValue();
            }
        }

        return Value;
    }
    
    protected abstract string Name { get; }
    protected string Value { get; set; }
    
    protected virtual string GetDefaultValue() => null;
}