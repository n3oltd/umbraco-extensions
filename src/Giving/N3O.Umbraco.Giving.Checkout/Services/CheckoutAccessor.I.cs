using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Giving.Checkout;

public interface ICheckoutAccessor {
    Entities.Checkout Get();
    Task<Entities.Checkout> GetAsync(CancellationToken cancellationToken = default);
    Task<Entities.Checkout> GetOrCreateAsync(CancellationToken cancellationToken, bool refreshCulture = false);
}
