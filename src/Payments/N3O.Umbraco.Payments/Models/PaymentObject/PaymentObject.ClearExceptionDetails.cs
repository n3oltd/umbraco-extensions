namespace N3O.Umbraco.Payments.Models;

public partial class PaymentObject {
    public void ClearExceptionDetails() {
        ExceptionDetails = null;
    }
}
