using N3O.Umbraco.Templates;
using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public class TemplateRenderer : ITemplateRenderer {
    private readonly ITemplateEngine _templateEngine;

    public TemplateRenderer(ITemplateEngine templateEngine) {
        _templateEngine = templateEngine;
    }

    public string Parse<T>(string source, T model, bool isHtml = true) {
        var rendered = _templateEngine.Render(source, model);

        return rendered;
    }

    public Task<string> ParseAsync<T>(string source, T model, bool isHtml = true) {
        return Task.FromResult(Parse(source, model, isHtml));
    }
}
