using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface ILegacyGivingTreeReader {
    string BuildPath(IContent content);
    int CountOfType(string contentTypeAlias);
    IReadOnlyList<IContentType> GetFormContentTypes();
    IReadOnlyList<LegacyForm> GetForms();
    IReadOnlyList<IContent> GetUpsellOffers();
}
