using Newtonsoft.Json;

namespace N3O.Umbraco.Accounts.Models;

public class Email : Value, IEmail {
    [JsonConstructor]
    public Email(string address) {
        Address = address?.Trim();
    }

    public Email(IEmail email) : this(email.Address) { }

    public string Address { get; }
}
