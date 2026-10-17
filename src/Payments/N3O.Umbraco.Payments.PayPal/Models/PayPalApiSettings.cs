namespace N3O.Umbraco.Payments.PayPal.Models;

public class PayPalApiSettings {
    public PayPalApiSettings(string baseUrl, string secret, string clientId) {
        BaseUrl = baseUrl;
        ClientId = clientId;
        Secret = secret;
    }

    public string BaseUrl { get; }
    public string ClientId { get; }
    public string Secret { get; }
}
