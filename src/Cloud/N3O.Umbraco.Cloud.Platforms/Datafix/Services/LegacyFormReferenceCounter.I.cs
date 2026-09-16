using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ILegacyFormReferenceCounter {
    IReadOnlyDictionary<Guid, int> CountReferences(IReadOnlyCollection<Guid> formKeys);
}
