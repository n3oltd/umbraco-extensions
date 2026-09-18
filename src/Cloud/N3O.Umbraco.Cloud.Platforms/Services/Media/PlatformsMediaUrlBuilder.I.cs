using Flurl;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsMediaUrlBuilder {
    Url Build(string url);
}
