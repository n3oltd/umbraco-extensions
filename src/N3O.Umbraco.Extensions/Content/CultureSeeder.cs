using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
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

    private readonly ILogger _logger;
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

    public void Seed(IContent content, int userId) {
        Seed(content, _localizationService.GetDefaultLanguageIsoCode(), userId);
    }

    public void SeedAll() {
        SeedAll(_localizationService.GetDefaultLanguageIsoCode());
    }

    public void SeedAll(string sourceCulture) {
        var roots = _contentService.GetRootContent()
                                   .Where(x => SeededRootAliases.Contains(x.ContentType.Alias, true))
                                   .ToList();

        foreach (var root in roots) {
            var descendants = _contentService.GetPagedDescendants(root.Id, 0, int.MaxValue, out _);

            foreach (var content in descendants.Prepend(root)) {
                if (content.ContentType.VariesByCulture() && !content.IsCultureAvailable(sourceCulture)) {
                    _logger.LogWarning("Could not seed the missing cultures of content {ID} as it has no {Culture} " +
                                       "version",
                                       content.Key,
                                       sourceCulture);
                }

                Seed(content, sourceCulture, UmbracoConstants.Security.SuperUserId);
            }
        }
    }

    private IReadOnlyList<string> AddMissingCultures(IContent content, string sourceCulture, bool fromPublished) {
        var addedCultures = new List<string>();
        var name = fromPublished ? content.GetPublishName(sourceCulture) : content.GetCultureName(sourceCulture);

        foreach (var language in _localizationService.GetAllLanguages()) {
            if (!content.IsCultureAvailable(language.IsoCode)) {
                content.SetCultureName(name, language.IsoCode);

                foreach (var property in content.Properties.Where(x => x.PropertyType.VariesByCulture())) {
                    var value = content.GetValue(property.Alias, sourceCulture, null, fromPublished);

                    content.SetValue(property.Alias, WithNewKeys(property, value), language.IsoCode);
                }

                addedCultures.Add(language.IsoCode);
            }
        }

        return addedCultures;
    }

    // A new node has no path until it is saved, so its root is found through its parent.
    private IContent GetRoot(IContent content) {
        if (content.ParentId == UmbracoConstants.System.Root) {
            return content;
        }

        var parent = _contentService.GetById(content.ParentId);
        var rootId = int.Parse(parent.Path.Split(',')[1]);

        return _contentService.GetById(rootId);
    }

    private bool IsInScope(IContent content) {
        if (!content.ContentType.VariesByCulture() || content.Trashed) {
            return false;
        }

        var root = GetRoot(content);

        return SeededRootAliases.Contains(root.ContentType.Alias, true);
    }

    // A culture keeps its publish info after the whole node is unpublished, so only a published node counts as
    // having its source culture published. Every culture of such a node is then published, including ones a uSync
    // import unpublished or an earlier save left as drafts.
    private void Seed(IContent content, string sourceCulture, int userId) {
        if (Seeding.Value || !IsInScope(content) || !content.IsCultureAvailable(sourceCulture)) {
            return;
        }

        Seeding.Value = true;

        try {
            var sourcePublished = content.Published && content.IsCulturePublished(sourceCulture);
            var addedCultures = AddMissingCultures(content, sourceCulture, sourcePublished);

            if (sourcePublished) {
                var unpublishedCultures = _localizationService.GetAllLanguages()
                                                              .Select(x => x.IsoCode)
                                                              .Where(x => content.IsCultureAvailable(x) &&
                                                                          !content.IsCulturePublished(x))
                                                              .ToArray();

                if (unpublishedCultures.Any()) {
                    var result = _contentService.SaveAndPublish(content, unpublishedCultures, userId);

                    if (!result.Success) {
                        _logger.LogWarning("Could not publish the seeded cultures of content {ID}: {Result}",
                                           content.Key,
                                           result.Result);
                    }
                }
            } else if (addedCultures.Any()) {
                _contentService.Save(content, userId);
            }
        } finally {
            Seeding.Value = false;
        }
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
