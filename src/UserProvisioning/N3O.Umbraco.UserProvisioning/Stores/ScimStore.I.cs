using N3O.Umbraco.UserProvisioning.Scim;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.UserProvisioning.Stores;

public interface IScimStore<TResource> where TResource : ScimResource {
    Task<TResource> CreateAsync(TResource resource);
    Task DeleteAsync(string id);
    Task<TResource> GetAsync(string id);
    Task<ScimListResponse<TResource>> ListAsync(ScimQuery query);
    Task<TResource> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations);
    Task<TResource> ReplaceAsync(TResource resource);
}
