using N3O.Umbraco.Cloud.Platforms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyGivingTreeLock : ILegacyGivingTreeLock {
    private readonly ILegacyGivingTreeReader _reader;
    private readonly IContentTypeService _contentTypeService;

    public LegacyGivingTreeLock(ILegacyGivingTreeReader reader, IContentTypeService contentTypeService) {
        _reader = reader;
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

        res.Locked = open.Count == 0;
        res.LockedContentTypes = [];
        res.AlreadyLockedContentTypes = locked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return res;
    }

    public GivingMigrationLockRes Lock() {
        var res = new GivingMigrationLockRes();
        var locked = new List<string>();
        var alreadyLocked = new List<string>();

        foreach (var contentType in GetContentTypesToLock()) {
            if (!HasAllowedChildren(contentType)) {
                alreadyLocked.Add(contentType.Alias);

                continue;
            }

            contentType.AllowedContentTypes = [];

            _contentTypeService.Save(contentType);

            locked.Add(contentType.Alias);
        }

        res.Locked = true;
        res.LockedContentTypes = locked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        res.AlreadyLockedContentTypes = alreadyLocked.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return res;
    }

    private IReadOnlyList<IContentType> GetContentTypesToLock() {
        var formContentTypes = _reader.GetFormContentTypes();

        var formAliases = formContentTypes.Select(x => x.Alias)
                                          .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var contentTypes = new Dictionary<string, IContentType>(StringComparer.OrdinalIgnoreCase);

        foreach (var contentType in formContentTypes) {
            contentTypes[contentType.Alias] = contentType;
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
