using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using SeededCopies = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.Dictionary<string, string>>;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Content;

public class CultureSeeder : ICultureSeeder {
    private const string SeededCopiesKeyPrefix = "N3O.CultureSeeding.Copies.";

    private static readonly string[] LocalizerManagedAliases = {
        AliasHelper<TextContainerContent>.ContentTypeAlias(),
        AliasHelper<TextContainerFolderContent>.ContentTypeAlias(),
        AliasHelper<TextSettingsContent>.ContentTypeAlias()
    };

    private static readonly string[] SeededRootAliases = { "settings", "template" };

    // Publishing seeded content raises its publish notifications again, and those must not seed it a second time.
    private static readonly AsyncLocal<bool> Seeding = new();

    private readonly ILogger<CultureSeeder> _logger;
    private readonly IContentService _contentService;
    private readonly IEntityService _entityService;
    private readonly ILocalizationService _localizationService;
    private readonly IKeyValueService _keyValueService;
    private readonly NestedContentKeys _nestedContentKeys = new();

    public CultureSeeder(ILogger<CultureSeeder> logger,
                         IContentService contentService,
                         IEntityService entityService,
                         ILocalizationService localizationService,
                         IKeyValueService keyValueService) {
        _logger = logger;
        _contentService = contentService;
        _entityService = entityService;
        _localizationService = localizationService;
        _keyValueService = keyValueService;
    }

    public bool Seed(IContent content, int userId) {
        return Seed(content, _localizationService.GetDefaultCultureCode(), userId, false, []);
    }

    public void SeedAll() {
        SeedAll(_localizationService.GetDefaultCultureCode());
    }

    public void SeedAll(string sourceCulture) {
        SeedAll(sourceCulture, [], false);
    }

    public void SeedAll(IEnumerable<int> newlyVaryingPropertyTypeIds) {
        SeedAll(_localizationService.GetDefaultCultureCode(), newlyVaryingPropertyTypeIds.ToList(), false);
    }

    public void SeedAllAndRepublish() {
        SeedAll(_localizationService.GetDefaultCultureCode(), [], true);
    }

    private IReadOnlyList<string> AddMissingCultures(IContent content, string sourceCulture) {
        var addedCultures = new List<string>();
        var name = content.GetPublishName(sourceCulture);

        foreach (var culture in _localizationService.GetAllCultureCodes()) {
            if (!content.IsCultureAvailable(culture)) {
                content.SetCultureName(name, culture);

                foreach (var property in content.Properties.Where(x => x.PropertyType.VariesByCulture())) {
                    CopyValue(content, property, sourceCulture, culture, true);
                }

                addedCultures.Add(culture);
            }
        }

        return addedCultures;
    }

    // Umbraco moves the value of a property that starts varying by culture into the default culture only, which
    // leaves it empty in every other culture the content already has.
    private IReadOnlyList<string> CopyNewlyVaryingValues(IContent content,
                                                         string sourceCulture,
                                                         bool fromPublished,
                                                         IReadOnlyList<int> newlyVaryingPropertyTypeIds,
                                                         SeededCopies seededCopies) {
        var properties = content.Properties
                                .Where(x => newlyVaryingPropertyTypeIds.Contains(x.PropertyType.Id))
                                .ToList();

        if (!properties.Any()) {
            return [];
        }

        var cultures = content.AvailableCultures.Where(x => !x.EqualsInvariant(sourceCulture)).ToList();

        foreach (var culture in cultures) {
            if (!seededCopies.ContainsKey(culture)) {
                seededCopies[culture] = new Dictionary<string, string>();
            }

            foreach (var property in properties) {
                var value = CopyValue(content, property, sourceCulture, culture, fromPublished);

                seededCopies[culture][property.Alias] = Fingerprint(value);
            }
        }

        return cultures;
    }

    private object CopyValue(IContent content,
                             IProperty property,
                             string sourceCulture,
                             string culture,
                             bool fromPublished) {
        var value = WithNewKeys(property, content.GetValue(property.Alias, sourceCulture, null, fromPublished));

        content.SetValue(property.Alias, value, culture);

        return value;
    }

    private string Fingerprint(object value) {
        return Convert.ToString(value, CultureInfo.InvariantCulture).Sha256();
    }

    // Publishing a culture publishes all of its pending edits, which are not ours to publish, so only a culture that
    // is already published and whose every pending edit is a value copied here is published.
    private IReadOnlyList<string> GetCulturesPendingOnlySeededCopies(IContent content, SeededCopies seededCopies) {
        return seededCopies.Where(x => content.IsCulturePublished(x.Key) &&
                                       HasOnlySeededEdits(content, x.Key, x.Value))
                           .Select(x => x.Key)
                           .ToList();
    }

    private IContent GetRoot(IContent content) {
        var rootId = int.Parse(content.Path.Split(',')[1]);

        return rootId == content.Id ? content : _contentService.GetById(rootId);
    }

    private SeededCopies GetSeededCopies(IContent content) {
        var json = _keyValueService.GetValue(GetSeededCopiesKey(content));

        return json.HasValue() ? JsonConvert.DeserializeObject<SeededCopies>(json) : new SeededCopies();
    }

    private string GetSeededCopiesKey(IContent content) {
        return SeededCopiesKeyPrefix + content.Key;
    }

    private IReadOnlyList<string> GetUnpublishedCultures(IContent content) {
        return _localizationService.GetAllCultureCodes()
                                   .Where(x => content.IsCultureAvailable(x) && !content.IsCulturePublished(x))
                                   .ToList();
    }

    private bool HasOnlySeededEdits(IContent content,
                                    string culture,
                                    IReadOnlyDictionary<string, string> copiedFingerprints) {
        if (content.GetCultureName(culture) != content.GetPublishName(culture)) {
            return false;
        }

        var hasSeededEdits = false;

        foreach (var property in content.Properties) {
            foreach (var value in property.Values.Where(x => x.Culture.EqualsInvariant(culture))) {
                var edited = Fingerprint(value.EditedValue);

                if (edited == Fingerprint(value.PublishedValue)) {
                    continue;
                }

                if (value.Segment == null &&
                    copiedFingerprints.TryGetValue(property.Alias, out var copied) &&
                    copied == edited) {
                    hasSeededEdits = true;
                } else {
                    return false;
                }
            }
        }

        return hasSeededEdits;
    }

    private bool IsInScope(IContent content) {
        if (!content.ContentType.VariesByCulture() ||
            content.Trashed ||
            LocalizerManagedAliases.Contains(content.ContentType.Alias, true)) {
            return false;
        }

        var root = GetRoot(content);

        return SeededRootAliases.Contains(root.ContentType.Alias, true);
    }

    private bool IsPendingCopy(IContent content, string culture, string alias, string fingerprint) {
        var edited = Fingerprint(content.GetValue(alias, culture));

        return edited == fingerprint && edited != Fingerprint(content.GetValue(alias, culture, null, true));
    }

    // A copy is tracked only until it is published or an editor changes it, after which it belongs to the editor.
    private void SaveSeededCopies(IContent content, SeededCopies seededCopies) {
        foreach (var culture in seededCopies.Keys.ToList()) {
            var pendingCopies = seededCopies[culture].Where(x => IsPendingCopy(content, culture, x.Key, x.Value))
                                                     .ToDictionary(x => x.Key, x => x.Value);

            if (pendingCopies.Any()) {
                seededCopies[culture] = pendingCopies;
            } else {
                seededCopies.Remove(culture);
            }
        }

        var key = GetSeededCopiesKey(content);
        var json = seededCopies.Any() ? JsonConvert.SerializeObject(seededCopies) : string.Empty;

        if (json != (_keyValueService.GetValue(key) ?? string.Empty)) {
            _keyValueService.SetValue(key, json);
        }
    }

    private bool Seed(IContent content,
                      string sourceCulture,
                      int userId,
                      bool republish,
                      IReadOnlyList<int> newlyVaryingPropertyTypeIds) {
        if (Seeding.Value || !IsInScope(content)) {
            return true;
        }

        if (!content.IsCultureAvailable(sourceCulture)) {
            _logger.LogWarning("Could not seed the missing cultures of content {ID} as it has no {Culture} version",
                               content.Key,
                               sourceCulture);

            return true;
        }

        Seeding.Value = true;

        try {
            var sourcePublished = content.Published && content.IsCulturePublished(sourceCulture);
            var seededCopies = GetSeededCopies(content);
            var copiedCultures = CopyNewlyVaryingValues(content,
                                                        sourceCulture,
                                                        sourcePublished,
                                                        newlyVaryingPropertyTypeIds,
                                                        seededCopies);
            var addedCultures = sourcePublished ? AddMissingCultures(content, sourceCulture) : [];
            var publishCultures = new List<string>();

            if (sourcePublished) {
                publishCultures.AddRange(addedCultures);
                publishCultures.AddRange(GetCulturesPendingOnlySeededCopies(content, seededCopies));

                if (republish) {
                    publishCultures.AddRange(GetUnpublishedCultures(content));
                }
            }

            var succeeded = true;

            if (publishCultures.Any()) {
                var result = _contentService.SaveAndPublish(content, publishCultures.Distinct().ToArray(), userId);

                succeeded = Succeeded(result.Success, result.Result, content);
            } else if (copiedCultures.Any() || addedCultures.Any()) {
                var result = _contentService.Save(content, userId);

                succeeded = Succeeded(result.Success, result.Result, content);
            }

            if (succeeded) {
                SaveSeededCopies(content, seededCopies);
            }

            return succeeded;
        } finally {
            Seeding.Value = false;
        }
    }

    private void SeedAll(string sourceCulture, IReadOnlyList<int> newlyVaryingPropertyTypeIds, bool republish) {
        var roots = _contentService.GetRootContent()
                                   .Where(x => SeededRootAliases.Contains(x.ContentType.Alias, true))
                                   .ToList();

        foreach (var root in roots) {
            var keys = _entityService.GetDescendants(root.Id, UmbracoObjectTypes.Document)
                                     .Select(x => x.Key)
                                     .Prepend(root.Key)
                                     .ToList();

            foreach (var key in keys) {
                try {
                    SeedLatest(key, sourceCulture, newlyVaryingPropertyTypeIds, republish);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to seed the missing cultures of content {ID}", key);
                }
            }
        }
    }

    private void SeedLatest(Guid key,
                            string sourceCulture,
                            IReadOnlyList<int> newlyVaryingPropertyTypeIds,
                            bool republish) {
        var content = _contentService.GetById(key);

        Seed(content, sourceCulture, UmbracoConstants.Security.SuperUserId, republish, newlyVaryingPropertyTypeIds);
    }

    private bool Succeeded(bool success, object result, IContent content) {
        if (!success) {
            _logger.LogError("Could not save the seeded cultures of content {ID}: {Result}", content.Key, result);
        }

        return success;
    }

    // Umbraco caches nested element values by element key alone, so a culture sharing another culture's keys would
    // be served that culture's values.
    private object WithNewKeys(IProperty property, object value) {
        if (value != null && property.PropertyType.IsNestedContent()) {
            return _nestedContentKeys.Regenerate(value.ToString());
        }

        return value;
    }
}
