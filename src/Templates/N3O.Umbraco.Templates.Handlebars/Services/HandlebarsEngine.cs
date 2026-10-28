using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace N3O.Umbraco.Templates.Handlebars;

public class HandlebarsEngine : ITemplateEngine {
    private readonly ILogger _logger;
    private readonly IHandlebarsCompiler _handlebarsCompiler;

    public HandlebarsEngine(ILogger<HandlebarsEngine> logger, IHandlebarsCompiler handlebarsCompiler) {
        _logger = logger;
        _handlebarsCompiler = handlebarsCompiler;
    }

    public bool IsSyntaxValid(string content) {
        var isValid = _handlebarsCompiler.IsSyntaxValid(content);

        return isValid;
    }

    public string Render(string markup, object model, IReadOnlyDictionary<string, string> partials = null) {
        return DoRender(markup, model, partials, false);
    }

    public string RenderText(string markup, object model, IReadOnlyDictionary<string, string> partials = null) {
        return DoRender(markup, model, partials, true);
    }

    private string DoRender(string markup,
                            object model,
                            IReadOnlyDictionary<string, string> partials,
                            bool noEscape) {
        try {
            var compiledHandlebars = _handlebarsCompiler.Compile(markup, partials, noEscape: noEscape);
            var rendered = compiledHandlebars(model);
        
            return rendered;
        } catch (HandlebarsRuntimeException ex) {
            _logger.LogError(ex, "Failed to render markup {Markup} with model {@Model}", markup, model);

            throw;
        }
    }
}
