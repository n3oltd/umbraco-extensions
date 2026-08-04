using N3O.Umbraco.Context;

namespace N3O.Umbraco.Giving.Checkout.Entities;

public partial class Checkout {
    public void UpdateCulture(ICultureAccessor cultureAccessor) {
        Culture = cultureAccessor.GetCulture();
    }
}
