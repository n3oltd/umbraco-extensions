using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Content;

public interface ICultureSeeder {
    void PublishSeeded(IContent content, IEnumerable<string> cultures);
    IReadOnlyList<string> Seed(IContent content);
    void SeedAll();
}
