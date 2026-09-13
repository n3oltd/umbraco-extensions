using Flurl;

namespace N3O.Umbraco.Utilities;

public interface IUrlBuilder {
    Url ProductionUrl(string url);
    Url Root();
}
