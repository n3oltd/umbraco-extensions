using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ILegacyGivingTreeReader {
    IReadOnlyList<IContentType> GetFormContentTypes();
    IReadOnlyList<LegacyForm> GetForms();
    int CountOfType(string contentTypeAlias);
}
