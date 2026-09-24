using N3O.Umbraco.Entities;

namespace N3O.Umbraco.UserProvisioning.Entities;

public class ScimUserState : Entity {
    public string ExternalId { get; private set; }
    public string FamilyName { get; private set; }
    public string GivenName { get; private set; }

    public void SetExternalId(string externalId) {
        ExternalId = externalId;
    }

    public void SetName(string givenName, string familyName) {
        FamilyName = familyName;
        GivenName = givenName;
    }

    public static ScimUserState Create(EntityId id) {
        return Entity.Create<ScimUserState>(id);
    }
}
