using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Financial;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public interface INisab {
    Task<PublishedNisab> GetAsync(CancellationToken cancellationToken = default);
    Task<Money> GetGoldNisabAsync(Currency currency, CancellationToken cancellationToken = default);
    Task<Money> GetNisabAsync(Currency currency, NisabType nisabType, CancellationToken cancellationToken = default);
    Task<Money> GetSilverNisabAsync(Currency currency, CancellationToken cancellationToken = default);
}