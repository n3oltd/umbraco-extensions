using System.Threading.Tasks;

namespace N3O.Umbraco.Security;

public interface IBackofficeUser {
    Task<bool> IsLoggedInAsync();
}
