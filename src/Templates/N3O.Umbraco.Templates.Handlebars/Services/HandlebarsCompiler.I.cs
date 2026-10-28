using HandlebarsDotNet;
using System.Collections.Generic;

namespace N3O.Umbraco.Templates.Handlebars;

public interface IHandlebarsCompiler {
    bool IsSyntaxValid(string markup);
    HandlebarsTemplate<object, object> Compile(string markup,
                                               IReadOnlyDictionary<string, string> partials = null,
                                               string cacheKey = null,
                                               bool noEscape = false);
}
