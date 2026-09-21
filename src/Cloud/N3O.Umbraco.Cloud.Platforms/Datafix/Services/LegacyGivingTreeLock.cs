using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyGivingTreeLock : ILegacyGivingTreeLock {
    private readonly ILegacyGivingTreeReader _reader;
    private readonly IGivingMigrationStore _store;
    private readonly IContentTypeService _contentTypeService;

    public LegacyGivingTreeLock(ILegacyGivingTreeReader reader,
                                IGivingMigrationStore store,
                                IContentTypeService contentTypeService) {
        _reader = reader;
        _store = store;
        _contentTypeService = contentTypeService;
    }

    public GivingMigrationLockRes GetStatus() {
        var res = new GivingMigrationLockRes();
        var open = new List<string>();
        var locked = new List<string>();

        foreach (var contentType in GetContentTypesToLock()) {
            if (HasAllowedChildren(contentType)) {
                open.Add(contentType.Alias);
            } else {
                locked.Add(contentType.Alias);
            }
        }

        res.Locked = locked.Count > 0 && open.Count == 0;

        if (locked.Count == 0 && open.Count == 0) {
            res.Message = "No legacy giving content types are present, so there is nothing to lock";
        }

        res.LockedContentTypes = [];
        res.AlreadyLockedContentTypes = locked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return res;
    }

    public GivingMigrationLockRes Lock() {
        var res = new GivingMigrationLockRes();
        var locked = new List<string>();
        var alreadyLocked = new List<string>();
        var snapshot = new Dictionary<string, IReadOnlyList<string>>(_store.GetLockSnapshot(),
                                                                    StringComparer.OrdinalIgnoreCase);

        var legacyAliases = GetLegacyAliases();

        foreach (var contentType in GetContentTypesToLock()) {
            // Only the legacy entries are taken. A type that allows a legacy form usually allows other things too,
            // and clearing the lot would be a permanent loss the moment anyone forgets to unlock.
            var removed = contentType.AllowedContentTypes.OrEmpty()
                                     .Where(x => legacyAliases.Contains(x.Alias))
                                     .OrderBy(x => x.SortOrder)
                                     .Select(x => x.Alias)
                                     .ToList();

            if (removed.Count == 0) {
                alreadyLocked.Add(contentType.Alias);

                continue;
            }

            snapshot[contentType.Alias] = removed;

            // The snapshot is the only record of what was taken, so it is persisted before the type is changed and
            // a failure part way through the loop still leaves every earlier entry restorable.
            _store.SaveLockSnapshot(snapshot);

            contentType.AllowedContentTypes = Resequence(contentType.AllowedContentTypes
                                                                    .Where(x => !legacyAliases.Contains(x.Alias)));

            _contentTypeService.Save(contentType);

            locked.Add(contentType.Alias);
        }

        res.Locked = locked.Count > 0 || alreadyLocked.Count > 0;

        if (!res.Locked) {
            res.Message = "No legacy giving content types are present, so there was nothing to lock";
        }

        res.LockedContentTypes = locked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        res.AlreadyLockedContentTypes = alreadyLocked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return res;
    }

    public GivingMigrationLockRes Unlock() {
        var snapshot = _store.GetLockSnapshot();
        var restored = new List<string>();
        var unrestored = new List<string>();

        foreach (var pair in snapshot) {
            var contentType = _contentTypeService.Get(pair.Key);

            if (contentType == null) {
                unrestored.AddRange(pair.Value);

                continue;
            }

            // Additive, because only the legacy entries were taken and anything else the type allows was left alone
            // and may since have changed.
            contentType.AllowedContentTypes = Resequence(contentType.AllowedContentTypes
                                                                    .OrEmpty()
                                                                    .Concat(BuildAllowed(pair.Value, unrestored)));

            _contentTypeService.Save(contentType);

            restored.Add(contentType.Alias);
        }

        _store.DeleteLockSnapshot();

        var res = new GivingMigrationLockRes();
        res.Locked = false;
        res.LockedContentTypes = [];
        res.AlreadyLockedContentTypes = [];
        res.RestoredContentTypes = restored.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        res.UnrestoredContentTypes = unrestored.Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        if (unrestored.Count > 0) {
            res.Message = "Some allowed children no longer exist and could not be restored, and the snapshot has " +
                          "now been deleted";
        }

        return res;
    }

    // The legacy surface an editor could otherwise keep adding to: the option, folder and price handle types named
    // in the constants, plus whichever types this site actually uses as its forms.
    private IReadOnlyCollection<string> GetLegacyAliases() {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            GivingMigrationConstants.Legacy.DonationFormAlias,
            GivingMigrationConstants.Legacy.DonationFormFolderAlias,
            GivingMigrationConstants.Legacy.FeedbackDonationOptionAlias,
            GivingMigrationConstants.Legacy.FundDonationOptionAlias,
            GivingMigrationConstants.Legacy.PriceHandleAlias,
            GivingMigrationConstants.Legacy.SponsorshipDonationOptionAlias,
            GivingMigrationConstants.Legacy.UpsellOfferAlias
        };

        foreach (var contentType in _reader.GetFormContentTypes()) {
            aliases.Add(contentType.Alias);
        }

        return aliases;
    }

    private static IReadOnlyList<ContentTypeSort> Resequence(IEnumerable<ContentTypeSort> allowed) {
        var resequenced = new List<ContentTypeSort>();

        foreach (var entry in allowed.DistinctBy(x => x.Alias)) {
            resequenced.Add(new ContentTypeSort(entry.Id, resequenced.Count, entry.Alias));
        }

        return resequenced;
    }

    private IReadOnlyList<ContentTypeSort> BuildAllowed(IEnumerable<string> aliases, ICollection<string> unrestored) {
        var allowed = new List<ContentTypeSort>();

        foreach (var alias in aliases) {
            var child = _contentTypeService.Get(alias);

            if (child == null) {
                unrestored.Add(alias);

                continue;
            }

            allowed.Add(new ContentTypeSort(new Lazy<int>(() => child.Id), allowed.Count, child.Alias));
        }

        return allowed;
    }

    // Locking clears the very property this set is discovered from, so the snapshot has to be folded in or a locked
    // tree would report only the form types themselves.
    private IReadOnlyList<IContentType> GetContentTypesToLock() {
        var formContentTypes = _reader.GetFormContentTypes();

        var formAliases = formContentTypes.Select(x => x.Alias)
                                          .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var contentTypes = new Dictionary<string, IContentType>(StringComparer.OrdinalIgnoreCase);

        foreach (var contentType in formContentTypes) {
            contentTypes[contentType.Alias] = contentType;
        }

        foreach (var alias in _store.GetLockSnapshot().Keys) {
            if (contentTypes.ContainsKey(alias)) {
                continue;
            }

            var contentType = _contentTypeService.Get(alias);

            if (contentType != null) {
                contentTypes[alias] = contentType;
            }
        }

        foreach (var contentType in _contentTypeService.GetAll()) {
            if (contentTypes.ContainsKey(contentType.Alias)) {
                continue;
            }

            var allowed = contentType.AllowedContentTypes;

            if (allowed != null && allowed.Any(x => formAliases.Contains(x.Alias))) {
                contentTypes[contentType.Alias] = contentType;
            }
        }

        return contentTypes.Values.ToList();
    }

    private static bool HasAllowedChildren(IContentType contentType) {
        return contentType.AllowedContentTypes?.Any() == true;
    }
}
