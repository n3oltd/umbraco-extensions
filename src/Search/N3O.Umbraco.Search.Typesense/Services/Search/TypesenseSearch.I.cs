using N3O.Umbraco.Search.Typesense.Models;
using System.Threading.Tasks;

namespace N3O.Umbraco.Search.Typesense;

public interface ITypesenseSearch<T> where T : SearchDocument {
    Task ApplyAsync(ITypesenseSearchBuilder<T> q);
}
