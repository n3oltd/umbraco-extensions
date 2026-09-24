using N3O.Umbraco.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Entities;

public class ScimGroupState : Entity {
    public string ExternalId { get; private set; }
    public IReadOnlyList<Guid> Members { get; private set; } = [];

    public void SetExternalId(string externalId) {
        ExternalId = externalId;
    }

    public void SetMembers(IEnumerable<Guid> members) {
        Members = (members ?? []).Distinct().OrderBy(x => x).ToList();
    }

    public static ScimGroupState Create(EntityId id) {
        return Entity.Create<ScimGroupState>(id);
    }
}
