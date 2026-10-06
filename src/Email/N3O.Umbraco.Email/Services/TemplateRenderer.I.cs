using System.Threading.Tasks;

namespace N3O.Umbraco.Email;

public interface ITemplateRenderer {
    string Parse<T>(string source, T model, bool isHtml = true);
    Task<string> ParseAsync<T>(string source, T model, bool isHtml = true);
}
