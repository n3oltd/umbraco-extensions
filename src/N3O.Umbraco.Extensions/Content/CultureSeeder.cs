using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Content;

public class CultureSeeder : ICultureSeeder {
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

    public void PublishSeeded(IContent content, IEnumerable<string> cultures) {
        var defaultCulture = _localizationService.GetDefaultLanguageIsoCode();

        if (content.IsCulturePublished(defaultCulture)) {
            var result = _contentService.SaveAndPublish(content, cultures.ToArray());

            if (!result.Success) {
                _logger.LogWarning("Could not publish the seeded cultures of content {ID}: {Result}",
                                   content.Key,
                                   result.Result);
            }
        }
    }

    // Content with no default culture version, such as a node first created in another language, has nothing to
    // seed from and is left as it is.
    public IReadOnlyList<string> Seed(IContent content) {
        var seededCultures = new List<string>();
        var defaultCulture = _localizationService.GetDefaultLanguageIsoCode();

        if (!IsInScope(content) || !content.IsCultureAvailable(defaultCulture)) {
            return seededCultures;
        }

        var fromPublished = content.IsCulturePublished(defaultCulture);
        var name = fromPublished ? content.GetPublishName(defaultCulture) : content.GetCultureName(defaultCulture);

        foreach (var language in _localizationService.GetAllLanguages()) {
            if (!content.IsCultureAvailable(language.IsoCode)) {
                content.SetCultureName(name, language.IsoCode);

                foreach (var property in content.Properties.Where(x => x.PropertyType.VariesByCulture())) {
                    var value = content.GetValue(property.Alias, defaultCulture, null, fromPublished);

                    content.SetValue(property.Alias, WithNewKeys(property, value), language.IsoCode);
                }

                seededCultures.Add(language.IsoCode);
            }
        }

        return seededCultures;
    }

    public void SeedAll() {
        var defaultCulture = _localizationService.GetDefaultLanguageIsoCode();
        var roots = _contentService.GetRootContent()
                                   .Where(x => SeededRootAliases.Contains(x.ContentType.Alias, true))
                                   .ToList();

        foreach (var root in roots) {
            var descendants = _contentService.GetPagedDescendants(root.Id, 0, int.MaxValue, out _);

            foreach (var content in descendants.Prepend(root)) {
                if (content.ContentType.VariesByCulture() && !content.IsCultureAvailable(defaultCulture)) {
                    _logger.LogWarning("Could not seed the missing cultures of content {ID} as it has no {Culture} " +
                                       "version",
                                       content.Key,
                                       defaultCulture);
                }

                var seededCultures = Seed(content);

                if (seededCultures.Any()) {
                    if (content.IsCulturePublished(defaultCulture)) {
                        PublishSeeded(content, seededCultures);
                    } else {
                        _contentService.Save(content);
                    }
                }
            }
        }
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

    // Umbraco caches nested element values by element key alone, so a culture sharing another culture's keys would
    // be served that culture's values.
    private object WithNewKeys(IProperty property, object value) {
        if (value != null && property.PropertyType.IsNestedContent()) {
            return _nestedContentKeys.Regenerate(value.ToString());
        }

        return value;
    }
}
