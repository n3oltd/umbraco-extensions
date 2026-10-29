using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Content;

public interface ICultureSeeder {
    bool Seed(IContent content, int userId);
    void SeedAll();
    void SeedAll(string sourceCulture);
    void SeedAll(IReadOnlyCollection<int> newlyVaryingPropertyTypeIds);
}
