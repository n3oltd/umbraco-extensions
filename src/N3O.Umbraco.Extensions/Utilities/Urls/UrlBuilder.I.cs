using Flurl;

namespace N3O.Umbraco.Utilities;

public interface IUrlBuilder {
    Url MediaUrl(string url);
    Url Root();
}
