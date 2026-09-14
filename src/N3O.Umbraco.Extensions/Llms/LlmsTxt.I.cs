using System.Threading.Tasks;

namespace N3O.Umbraco.Llms;

public interface ILlmsTxt {
    Task PublishAsync();
    void Remove();
}
