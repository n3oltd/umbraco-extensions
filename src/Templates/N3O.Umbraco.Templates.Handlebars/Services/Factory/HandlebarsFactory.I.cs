using HandlebarsDotNet;
using System.Collections.Generic;

namespace N3O.Umbraco.Templates.Handlebars;

public interface IHandlebarsFactory {
    public IHandlebars Create(IReadOnlyDictionary<string, string> partials = null, bool noEscape = false);
}
