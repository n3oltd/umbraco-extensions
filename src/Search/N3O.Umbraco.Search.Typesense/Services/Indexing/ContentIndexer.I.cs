using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Search.Typesense;

public interface IContentIndexer {
    Task IndexAsync(IPublishedContent content);
}
