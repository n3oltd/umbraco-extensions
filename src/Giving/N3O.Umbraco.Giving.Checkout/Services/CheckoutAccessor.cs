using AsyncKeyedLock;
using N3O.Umbraco.Context;
using N3O.Umbraco.Entities;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Cart;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.References;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Giving.Checkout;

public class CheckoutAccessor : ICheckoutAccessor {
    private readonly ICheckoutIdAccessor _checkoutIdAccessor;
    private readonly ICultureAccessor _cultureAccessor;
    private readonly AsyncKeyedLocker<string> _locker;
    private readonly IRepository<Entities.Checkout> _repository;
    private readonly Lazy<ICartAccessor> _cartAccessor;
    private readonly Lazy<ICounters> _counters;
    private readonly Lazy<ILookups> _lookups;
    private readonly Lazy<IRemoteIpAddressAccessor> _remoteIpAddressAccessor;

    public CheckoutAccessor(ICheckoutIdAccessor checkoutIdAccessor,
                            ICultureAccessor cultureAccessor,
                            AsyncKeyedLocker<string> locker,
                            IRepository<Entities.Checkout> repository,
                            Lazy<ICartAccessor> cartAccessor,
                            Lazy<ICounters> counters,
                            Lazy<ILookups> lookups,
                            Lazy<IRemoteIpAddressAccessor> remoteIpAddressAccessor) {
        _checkoutIdAccessor = checkoutIdAccessor;
        _cultureAccessor = cultureAccessor;
        _locker = locker;
        _repository = repository;
        _cartAccessor = cartAccessor;
        _counters = counters;
        _lookups = lookups;
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
    }

    public Entities.Checkout Get() {
        return GetAsync().GetAwaiter().GetResult();
    }

    public async Task<Entities.Checkout> GetAsync(CancellationToken cancellationToken = default) {
        var checkoutId = _checkoutIdAccessor.GetId();

        var checkout = await _repository.GetAsync(checkoutId, cancellationToken);

        return checkout;
    }
    
    public async Task<Entities.Checkout> GetOrCreateAsync(CancellationToken cancellationToken,
                                                          bool refreshCulture = false) {
        var checkoutId = _checkoutIdAccessor.GetId();
        
        using (await _locker.LockAsync(checkoutId.ToString(), cancellationToken)) {
            var checkout = await _repository.GetAsync(checkoutId, cancellationToken);

            if (checkout == null) {
                var cart = await _cartAccessor.Value.GetAsync(cancellationToken);

                if (!cart.IsEmpty()) {
                    checkout = await Entities.Checkout.CreateAsync(_counters.Value,
                                                                   _cultureAccessor,
                                                                   _lookups.Value,
                                                                   _remoteIpAddressAccessor.Value,
                                                                   checkoutId,
                                                                   cart);

                    await _repository.InsertAsync(checkout);
                }
            } else if (refreshCulture && !_cultureAccessor.GetCulture().EqualsInvariant(checkout.Culture)) {
                checkout.UpdateCulture(_cultureAccessor);

                await _repository.UpdateAsync(checkout, RevisionBehaviour.Unchanged);
            }

            return checkout;
        }
    }
}
