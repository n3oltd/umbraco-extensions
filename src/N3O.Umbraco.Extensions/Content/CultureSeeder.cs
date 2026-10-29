using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Content;

public class CultureSeeder : ICultureSeeder {
    // Saving seeded content raises its save notifications again, and those must not seed it a second time.
    private static readonly AsyncLocal<bool> Seeding = new();
    private static readonly string[] SeededRootAliases = { "settings", "template" };

    // The string localizer creates every culture of its own content when it reads or writes text.
    private static readonly string[] LocalizerManagedAliases = {
        AliasHelper<TextContainerContent>.ContentTypeAlias(),
        AliasHelper<TextContainerFolderContent>.ContentTypeAlias(),
        AliasHelper<TextSettingsContent>.ContentTypeAlias()
    };

    private readonly ILogger<CultureSeeder> _logger;
    private readonly IContentService _contentService;
    private readonly ILocalizationService _localizationService;
    private readonly NestedContentKeys _nestedContentKeys = new();

    public CultureSeeder(ILogger<CultureSeeder> logger,
                         IContentService contentService,
                         ILocalizationService localizationService) {
        _logger = logger;
        _contentService = contentService;
        _localizationService = localizationService;
    }

    public bool Seed(IContent content, int userId) {
        return Seed(content, _localizationService.GetDefaultLanguageIsoCode(), userId, false, []);
    }

    public void SeedAll() {
        SeedAll(_localizationService.GetDefaultLanguageIsoCode());
    }

    public void SeedAll(string sourceCulture) {
        SeedAll(sourceCulture, []);
    }

    public void SeedAll(IReadOnlyCollection<int> newlyVaryingPropertyTypeIds) {
        SeedAll(_localizationService.GetDefaultLanguageIsoCode(), newlyVaryingPropertyTypeIds);
    }

    private IReadOnlyList<string> AddMissingCultures(IContent content, string sourceCulture, bool fromPublished) {
        var addedCultures = new List<string>();
        var name = fromPublished ? content.GetPublishName(sourceCulture) : content.GetCultureName(sourceCulture);

        foreach (var culture in _localizationService.GetAllCultureCodes()) {
            if (!content.IsCultureAvailable(culture)) {
                content.SetCultureName(name, culture);

                foreach (var property in content.Properties.Where(x => x.PropertyType.VariesByCulture())) {
                    CopyValue(content, property, sourceCulture, culture, fromPublished);
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
                                                         IReadOnlyCollection<int> newlyVaryingPropertyTypeIds) {
        var properties = content.Properties
                                .Where(x => newlyVaryingPropertyTypeIds.Contains(x.PropertyType.Id))
                                .ToList();

        if (!properties.Any()) {
            return [];
        }

        var cultures = content.AvailableCultures.Where(x => !x.EqualsInvariant(sourceCulture)).ToList();

        foreach (var culture in cultures) {
            foreach (var property in properties) {
                CopyValue(content, property, sourceCulture, culture, fromPublished);
            }
        }

        return cultures;
    }

    private void CopyValue(IContent content,
                           IProperty property,
                           string sourceCulture,
                           string culture,
                           bool fromPublished) {
        var value = content.GetValue(property.Alias, sourceCulture, null, fromPublished);

        content.SetValue(property.Alias, WithNewKeys(property, value), culture);
    }

    private IContent GetRoot(IContent content) {
        var rootId = int.Parse(content.Path.Split(',')[1]);

        return rootId == content.Id ? content : _contentService.GetById(rootId);
    }

    private IReadOnlyList<string> GetUnpublishedCultures(IContent content) {
        return _localizationService.GetAllCultureCodes()
                                   .Where(x => content.IsCultureAvailable(x) && !content.IsCulturePublished(x))
                                   .ToList();
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

    // A culture keeps its publish info after the whole node is unpublished, so only a published node counts as
    // having its source culture published.
    private bool Seed(IContent content,
                      string sourceCulture,
                      int userId,
                      bool publishAllCultures,
                      IReadOnlyCollection<int> newlyVaryingPropertyTypeIds) {
        if (Seeding.Value || !IsInScope(content) || !content.IsCultureAvailable(sourceCulture)) {
            return true;
        }

        Seeding.Value = true;

        try {
            var sourcePublished = content.Published && content.IsCulturePublished(sourceCulture);
            var copiedCultures = CopyNewlyVaryingValues(content,
                                                        sourceCulture,
                                                        sourcePublished,
                                                        newlyVaryingPropertyTypeIds);
            var addedCultures = AddMissingCultures(content, sourceCulture, sourcePublished);

            if (sourcePublished) {
                var publishCultures = (publishAllCultures ? GetUnpublishedCultures(content) : addedCultures)
                                      .Union(copiedCultures, StringComparer.InvariantCultureIgnoreCase)
                                      .ToArray();

                if (publishCultures.Any()) {
                    var result = _contentService.SaveAndPublish(content, publishCultures, userId);

                    return Succeeded(result.Success, result.Result, content);
                }
            } else if (copiedCultures.Any() || addedCultures.Any()) {
                var result = _contentService.Save(content, userId);

                return Succeeded(result.Success, result.Result, content);
            }

            return true;
        } finally {
            Seeding.Value = false;
        }
    }

    private void SeedAll(string sourceCulture, IReadOnlyCollection<int> newlyVaryingPropertyTypeIds) {
        var roots = _contentService.GetRootContent()
                                   .Where(x => SeededRootAliases.Contains(x.ContentType.Alias, true))
                                   .ToList();

        foreach (var root in roots) {
            var ids = _contentService.GetPagedDescendants(root.Id, 0, int.MaxValue, out _)
                                     .Select(x => x.Id)
                                     .Prepend(root.Id)
                                     .ToList();

            foreach (var id in ids) {
                try {
                    SeedLatest(id, sourceCulture, newlyVaryingPropertyTypeIds);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to seed the missing cultures of content {ID}", id);
                }
            }
        }
    }

    private void SeedLatest(int id, string sourceCulture, IReadOnlyCollection<int> newlyVaryingPropertyTypeIds) {
        var content = _contentService.GetById(id);

        if (content.ContentType.VariesByCulture() && !content.IsCultureAvailable(sourceCulture)) {
            _logger.LogWarning("Could not seed the missing cultures of content {ID} as it has no {Culture} version",
                               content.Key,
                               sourceCulture);
        }

        Seed(content, sourceCulture, UmbracoConstants.Security.SuperUserId, true, newlyVaryingPropertyTypeIds);
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
