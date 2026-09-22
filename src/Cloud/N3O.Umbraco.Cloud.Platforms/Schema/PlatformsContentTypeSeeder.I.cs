using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsContentTypeSeeder {
    IReadOnlyList<string> Aliases { get; }
    void Seed();
}
