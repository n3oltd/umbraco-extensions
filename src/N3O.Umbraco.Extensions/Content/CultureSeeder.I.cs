using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Content;

public interface ICultureSeeder {
    void Seed(IContent content, int userId);
    void SeedAll();
    void SeedAll(string sourceCulture);
}
