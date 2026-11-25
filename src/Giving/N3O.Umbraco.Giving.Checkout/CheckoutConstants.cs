namespace N3O.Umbraco.Giving.Checkout;

public static class CheckoutConstants {
    public const string ApiName = "Checkout";
    public const string DevToolsApiName = "CheckoutDevTools";
    
    public static class BlockModuleKeys {
        public static readonly string CheckoutAccount = nameof(CheckoutAccount);
        public static readonly string CheckoutComplete = nameof(CheckoutComplete);
        public static readonly string CheckoutDonation = nameof(CheckoutDonation);
        public static readonly string CheckoutError = nameof(CheckoutError);
        public static readonly string CheckoutRegularGiving = nameof(CheckoutRegularGiving);
    }
}
