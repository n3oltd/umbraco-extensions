using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ILegacyGivingTreeReader {
    string BuildPath(IContent content);
    int CountOfType(string contentTypeAlias);
    IReadOnlyList<IContentType> GetFormContentTypes();
    IReadOnlyList<LegacyForm> GetForms();
    IReadOnlyList<IContent> GetUpsellOffers();
}
