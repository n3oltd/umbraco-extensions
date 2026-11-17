using System.Collections.Generic;

namespace N3O.Umbraco.Templates;

public interface ITemplateEngine {
    bool IsSyntaxValid(string content);
    string Render(string markup, object model, IReadOnlyDictionary<string, string> partials = null);
    string RenderText(string markup, object model, IReadOnlyDictionary<string, string> partials = null);
}
