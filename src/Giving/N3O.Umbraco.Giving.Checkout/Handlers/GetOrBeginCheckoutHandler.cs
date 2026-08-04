using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Checkout.Commands;
using N3O.Umbraco.Giving.Checkout.Models;
using N3O.Umbraco.Mediator;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Giving.Checkout.Handlers;

public class GetOrBeginCheckoutHandler : IRequestHandler<GetOrBeginCheckoutCommand, None, CheckoutRes> {
    private readonly ICheckoutAccessor _checkoutAccessor;
    private readonly IUmbracoMapper _mapper;

    public GetOrBeginCheckoutHandler(ICheckoutAccessor checkoutAccessor, IUmbracoMapper mapper) {
        _checkoutAccessor = checkoutAccessor;
        _mapper = mapper;
    }
    
    public async Task<CheckoutRes> Handle(GetOrBeginCheckoutCommand req, CancellationToken cancellationToken) {
        var checkout = await _checkoutAccessor.GetOrCreateAsync(cancellationToken: cancellationToken);
        
        var res = checkout.IfNotNull(_mapper.Map<Entities.Checkout, CheckoutRes>);

        return res;
    }
}
