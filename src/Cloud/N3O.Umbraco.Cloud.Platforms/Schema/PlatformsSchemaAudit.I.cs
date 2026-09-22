using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsSchemaAudit {
    IReadOnlyList<string> FindGaps();
}
