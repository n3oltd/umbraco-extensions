namespace N3O.Umbraco.Payments.Models;

public partial class PaymentObject {
    public void UnhandledError(string exceptionDetails, string message) {
        ExceptionDetails = exceptionDetails;
        
        Error(message);
    }
}
