using N3O.Umbraco.Search.Typesense.Models;
using System.Threading.Tasks;

namespace N3O.Umbraco.Search.Typesense;

public abstract class TypesenseSearch<T> : ITypesenseSearch<T> where T : SearchDocument {
    public Task ApplyAsync(ITypesenseSearchBuilder<T> q) {
        Q = q;

        return ApplyAsync();
    }

    protected abstract Task ApplyAsync();

    protected ITypesenseSearchBuilder<T> Q { get; private set; }
}
