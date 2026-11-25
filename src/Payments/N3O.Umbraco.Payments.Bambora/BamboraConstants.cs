namespace N3O.Umbraco.Payments.Bambora;

public static class BamboraConstants {
    public const string ApiName = "Bambora";
    public const string ApiBaseUrl = "https://api.bam.shift4api.net/v1";
    public static readonly BamboraPaymentMethod PaymentMethod = new();
    
    public static class Iso3CountryCodes {
        public static readonly string Canada = "CAN";
        public static readonly string UnitedStates = "USA";
    }
}
