using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyGivingTreeReader : ILegacyGivingTreeReader {
    private const string PlatformsPrefix = "platforms";

    private static readonly string[] OptionAliases = [
        GivingMigrationConstants.Legacy.FundDonationOptionAlias,
        GivingMigrationConstants.Legacy.SponsorshipDonationOptionAlias,
        GivingMigrationConstants.Legacy.FeedbackDonationOptionAlias
    ];

    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private HashSet<int> _formContentTypeIdsFromContent;

    public LegacyGivingTreeReader(IContentService contentService, IContentTypeService contentTypeService) {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public string BuildPath(IContent content) {
        var names = new List<string>();
        var current = _contentService.GetParent(content);

        while (current != null) {
            names.Insert(0, current.Name);

            current = _contentService.GetParent(current);
        }

        names.Add(content.Name);

        return string.Join(GivingMigrationConstants.PathSeparator, names);
    }

    public IReadOnlyList<IContentType> GetFormContentTypes() {
        return _contentTypeService.GetAll()
                                  .Where(IsLegacyFormContentType)
                                  .ToList();
    }

    public IReadOnlyList<LegacyForm> GetForms() {
        var optionTypeIds = GetOptionContentTypeIds();
        var forms = new List<LegacyForm>();

        foreach (var contentType in GetFormContentTypes()) {
            foreach (var content in GetAllOfType(contentType.Id)) {
                if (content.Trashed) {
                    continue;
                }

                var parent = _contentService.GetParent(content);

                var form = new LegacyForm();
                form.Content = content;
                form.FolderName = IsUsableFolder(parent) ? parent.Name : null;
                form.Path = BuildPath(content);
                form.Options = GetOptions(content.Id, optionTypeIds);

                forms.Add(form);
            }
        }

        return forms;
    }

    public IReadOnlyList<IContent> GetUpsellOffers() {
        var contentType = _contentTypeService.Get(GivingMigrationConstants.Legacy.UpsellOfferAlias);

        if (contentType == null) {
            return [];
        }

        return GetAllOfType(contentType.Id).Where(x => !x.Trashed).ToList();
    }

    public int CountOfType(string contentTypeAlias) {
        var contentType = _contentTypeService.Get(contentTypeAlias);

        return contentType == null ? 0 : GetAllOfType(contentType.Id).Count(x => !x.Trashed);
    }

    private bool IsLegacyFormContentType(IContentType contentType) {
        if (contentType.Alias.StartsWith(PlatformsPrefix, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (contentType.ContentTypeComposition
                       .Any(x => x.Alias.StartsWith(PlatformsPrefix, StringComparison.OrdinalIgnoreCase))) {
            return false;
        }

        var allowed = contentType.AllowedContentTypes;

        if (allowed != null && allowed.Any(x => OptionAliases.Contains(x.Alias, StringComparer.OrdinalIgnoreCase))) {
            return true;
        }

        return GetFormContentTypeIdsFromContent().Contains(contentType.Id);
    }

    // A form type whose AllowedContentTypes were already cleared by the tree lock is only discoverable from the
    // options that still point at it.
    private HashSet<int> GetFormContentTypeIdsFromContent() {
        if (_formContentTypeIdsFromContent != null) {
            return _formContentTypeIdsFromContent;
        }

        var contentTypeIds = new HashSet<int>();
        var parentIds = new HashSet<int>();

        foreach (var optionTypeId in GetOptionContentTypeIds()) {
            foreach (var option in GetAllOfType(optionTypeId)) {
                if (!option.Trashed) {
                    parentIds.Add(option.ParentId);
                }
            }
        }

        if (parentIds.Count > 0) {
            foreach (var parent in _contentService.GetByIds(parentIds.ToList())) {
                contentTypeIds.Add(parent.ContentTypeId);
            }
        }

        _formContentTypeIdsFromContent = contentTypeIds;

        return _formContentTypeIdsFromContent;
    }

    private IReadOnlyList<int> GetOptionContentTypeIds() {
        return OptionAliases.Select(x => _contentTypeService.Get(x))
                            .Where(x => x != null)
                            .Select(x => x.Id)
                            .ToList();
    }

    private IReadOnlyList<IContent> GetOptions(int formId, IReadOnlyList<int> optionTypeIds) {
        if (optionTypeIds.Count == 0) {
            return [];
        }

        return GivingMigrationContent.GetChildren(_contentService, formId)
                                     .Where(x => optionTypeIds.Contains(x.ContentTypeId))
                                     .ToList();
    }

    private IReadOnlyList<IContent> GetAllOfType(int contentTypeId) {
        return GivingMigrationContent.GetAllOfType(_contentService, contentTypeId);
    }

    private bool IsUsableFolder(IContent parent) {
        if (parent == null || string.IsNullOrWhiteSpace(parent.Name)) {
            return false;
        }

        var contentType = _contentTypeService.Get(parent.ContentTypeId);

        if (contentType == null) {
            return false;
        }

        if (string.Equals(contentType.Alias,
                          GivingMigrationConstants.Legacy.DonationFormsAlias,
                          StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return !IsLegacyFormContentType(contentType);
    }
}
