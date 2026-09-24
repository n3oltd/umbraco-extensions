using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Lookups;

public abstract class LookupsCollection<T> : ILookupsCollection<T> where T : ILookup {
    private DateTime _nextReloadAt = DateTime.MinValue;
    private Snapshot _snapshot;

    public virtual async Task<T> FindByIdAsync(string id, CancellationToken cancellationToken = default) {
        var snapshot = await GetSnapshotAsync(cancellationToken);

        snapshot.IdDictionary.TryGetValue(id, out var lookup);

        return lookup;
    }

    public virtual async Task<IEnumerable<T>> FindByNameAsync(string name,
                                                              CancellationToken cancellationToken = default) {
        if (!typeof(T).ImplementsInterface<INamedLookup>()) {
            throw new Exception($"{typeof(T).GetFriendlyName()} does not implement {nameof(INamedLookup)} so cannot be searched by name");
        }

        var snapshot = await GetSnapshotAsync(cancellationToken);

        snapshot.NameDictionary.TryGetValue(name, out var lookups);

        return lookups.OrEmpty();
    }

    async Task<ILookup> ILookupsCollection.FindByIdAsync(string id, CancellationToken cancellationToken) {
        var lookup = await FindByIdAsync(id, cancellationToken);

        return lookup;
    }

    async Task<IEnumerable<ILookup>> ILookupsCollection.FindByNameAsync(string name,
                                                                        CancellationToken cancellationToken) {
        var lookups = await FindByNameAsync(name, cancellationToken);

        return lookups.Cast<ILookup>().ToList();
    }

    async Task<IReadOnlyList<ILookup>> ILookupsCollection.GetAllAsync(CancellationToken cancellationToken) {
        var all = await GetAllAsync(cancellationToken);

        return all.Cast<ILookup>().ToList();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) {
        var snapshot = await GetSnapshotAsync(cancellationToken);

        return snapshot.All;
    }

    private async Task<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken) {
        if (DateTime.UtcNow <= _nextReloadAt) {
            return _snapshot;
        } else if (CanReload()) {
            var all = await LoadAllAsync(cancellationToken);

            Reload(all);

            _nextReloadAt = DateTime.UtcNow.Add(ReloadInterval);

            return _snapshot;
        } else if (_snapshot != null) {
            return _snapshot;
        } else {
            var all = await LoadAllAsync(cancellationToken);

            return new Snapshot(all);
        }
    }

    protected virtual bool CanReload() {
        return true;
    }

    protected abstract Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken);

    protected void MarkStale() {
        _nextReloadAt = DateTime.MinValue;
    }

    protected void Reload(IEnumerable<T> all) {
        _snapshot = new Snapshot(all);
    }

    protected virtual TimeSpan ReloadInterval => TimeSpan.FromMinutes(5);

    private class Snapshot {
        public Snapshot(IEnumerable<T> all) {
            All = all.OrEmpty().ToList();
            IdDictionary = All.ToDictionary(x => x.Id, x => x, StringComparer.InvariantCultureIgnoreCase);

            if (typeof(T).ImplementsInterface<INamedLookup>()) {
                NameDictionary = All.GroupBy(x => ((INamedLookup) x).Name.ToLowerInvariant())
                                    .ToDictionary(x => x.Key,
                                                  x => (IReadOnlyList<T>) x.ToList(),
                                                  StringComparer.InvariantCultureIgnoreCase);
            }
        }

        public IReadOnlyList<T> All { get; }
        public Dictionary<string, T> IdDictionary { get; }
        public Dictionary<string, IReadOnlyList<T>> NameDictionary { get; }
    }
}
