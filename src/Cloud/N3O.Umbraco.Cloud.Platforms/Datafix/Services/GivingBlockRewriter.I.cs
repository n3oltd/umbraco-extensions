using N3O.Umbraco.Cloud.Platforms.Models;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingBlockRewriter {
    IReadOnlyList<GivingMigrationIssueRes> FindReferences(IReadOnlyCollection<Guid> legacyIds);
    GivingMigrationRepointRes Repoint(RepointGivingBlocksReq req);
    GivingMigrationRewriteRes Rewrite(RewriteGivingBlocksReq req);
}
