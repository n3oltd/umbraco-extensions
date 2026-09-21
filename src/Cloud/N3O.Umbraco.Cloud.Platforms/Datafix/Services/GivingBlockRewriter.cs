using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;
using UmbracoSecurity = Umbraco.Cms.Core.Constants.Security;
using UmbracoSystem = Umbraco.Cms.Core.Constants.System;

namespace N3O.Umbraco.Cloud.Platforms;

// Rewrites the legacy donation form picker value stored inside a page's blocks into the platforms campaign picker
// value, so the blocks render the migrated campaign instead of the legacy form.
// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingBlockRewriter : IGivingBlockRewriter {
    private const int PageSize = 200;
    private const string DocumentUdiPrefix = "umb://document/";
    private const string NestedContentTypeAlias = "ncContentTypeAlias";
    private const string NestedKey = "key";
    private const string NestedName = "name";

    private static readonly Regex DocumentUdi = new("^umb://document/([0-9a-fA-F]{32})$",
                                                    RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AnyDocumentUdi = new("umb://document/([0-9a-fA-F]{32})",
                                                       RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly Dictionary<string, bool> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly IGivingMigrationStore _store;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly ILogger<GivingBlockRewriter> _logger;

    public GivingBlockRewriter(IGivingMigrationStore store,
                               IContentService contentService,
                               IContentTypeService contentTypeService,
                               IDataTypeEditor dataTypeEditor,
                               ILogger<GivingBlockRewriter> logger) {
        _store = store;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _dataTypeEditor = dataTypeEditor;
        _logger = logger;
    }

    public GivingMigrationRepointRes Repoint(RepointGivingBlocksReq req) {
        var res = new GivingMigrationRepointRes();
        res.Preview = req.Preview;

        var aliases = req.ContentTypeAliases.OrEmpty().Where(x => x.HasValue()).ToList();

        if (aliases.Count == 0) {
            res.Message = "No block content type aliases were supplied";

            return res;
        }

        var propertyAlias = req.PropertyAlias.HasValue()
                                ? req.PropertyAlias
                                : GivingMigrationConstants.Legacy.DonationFormAlias;

        var dataType = FindDonationFormDataType(out var problem);

        if (dataType == null) {
            res.Message = problem;

            return res;
        }

        var items = aliases.Select(x => RepointOne(x, propertyAlias, dataType, req.Preview)).ToList();

        res.Items = items;
        res.Attempted = items.Count;
        res.Repointed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Rewritten);
        res.Skipped = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Skipped);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);

        return res;
    }

    public GivingMigrationRewriteRes Rewrite(RewriteGivingBlocksReq req) {
        var res = new GivingMigrationRewriteRes();
        res.Preview = req.Preview;

        var propertyAliases = req.PropertyAliases.OrEmpty().Where(x => x.HasValue()).ToList();

        if (propertyAliases.Count == 0) {
            propertyAliases.Add(GivingMigrationConstants.Legacy.DonationFormAlias);
        }

        var itemContentType = _contentTypeService.Get(req.ItemContentTypeAlias);

        if (itemContentType == null) {
            res.Message = "The donation form item element type does not exist: " + req.ItemContentTypeAlias;

            return res;
        }

        var dataType = FindDonationFormDataType(out var problem);

        if (dataType == null) {
            res.Message = problem;

            return res;
        }

        var campaigns = _store.GetLedger()
                              .Where(x => x.Kind == GivingMigrationConstants.LedgerKinds.Campaign)
                              .ToDictionary(x => x.LegacyId, x => x);

        if (campaigns.Count == 0) {
            res.Message = "The migration ledger is empty so no legacy form can be resolved to a campaign";

            return res;
        }

        var items = new List<GivingMigrationRewriteItemRes>();
        var issues = new List<GivingMigrationIssueRes>();

        foreach (var content in GetAllContent()) {
            res.PagesScanned++;

            var item = RewriteOne(content,
                                  false,
                                  propertyAliases,
                                  campaigns,
                                  itemContentType.Alias,
                                  dataType.Key,
                                  req.Preview,
                                  issues);

            if (item != null) {
                items.Add(item);
            }
        }

        // Blueprints are a separate node type and are not returned by the descendant walk, so a blueprint would keep
        // its legacy reference and hand it to every page later created from it.
        foreach (var blueprint in _contentService.GetBlueprintsForContentTypes()) {
            res.PagesScanned++;

            var item = RewriteOne(blueprint,
                                  true,
                                  propertyAliases,
                                  campaigns,
                                  itemContentType.Alias,
                                  dataType.Key,
                                  req.Preview,
                                  issues);

            if (item != null) {
                items.Add(item);
            }
        }

        res.Items = items;
        res.Issues = issues;
        res.PagesMatched = items.Count;
        res.PagesRewritten = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Rewritten);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.ReferencesFound = items.Sum(x => x.References);
        res.ReferencesRewritten = items.Sum(x => x.Rewritten);
        res.ReferencesUnmapped = issues.Count(x => x.Kind == GivingMigrationConstants.IssueKinds.UnmappedReference);

        return res;
    }

    // Campaign and offering parity says nothing about whether any content still points at the legacy tree, so the
    // purge asks for the references themselves. Every value is scanned rather than only the picker properties,
    // because deleting a node breaks a reference to it wherever it is held, and the published value is scanned
    // alongside the draft because a page can still serve the legacy reference after its draft was rewritten.
    public IReadOnlyList<GivingMigrationIssueRes> FindReferences(IReadOnlyCollection<Guid> legacyIds) {
        var issues = new List<GivingMigrationIssueRes>();

        if (legacyIds.Count == 0) {
            return issues;
        }

        foreach (var content in GetAllContent()) {
            FindReferences(content, legacyIds, issues);
        }

        foreach (var blueprint in _contentService.GetBlueprintsForContentTypes()) {
            FindReferences(blueprint, legacyIds, issues);
        }

        return issues;
    }

    // The property is edited in place rather than through a content type designer, because the designer rebuilds a
    // type from a full declaration and these block types belong to the site, not to this package.
    private GivingMigrationRepointItemRes RepointOne(string contentTypeAlias,
                                                     string propertyAlias,
                                                     IDataType dataType,
                                                     bool preview) {
        var item = new GivingMigrationRepointItemRes();
        item.ContentTypeAlias = contentTypeAlias;
        item.PropertyAlias = propertyAlias;
        item.ToEditorAlias = dataType.EditorAlias;

        try {
            var contentType = _contentTypeService.Get(contentTypeAlias);

            if (contentType == null) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.Message = "The content type does not exist";

                return item;
            }

            var property = contentType.PropertyTypes.FirstOrDefault(x => x.Alias.EqualsInvariant(propertyAlias));

            if (property == null) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.Message = "The content type has no property named " + propertyAlias.Quote();

                return item;
            }

            item.FromEditorAlias = property.PropertyEditorAlias;

            if (property.DataTypeKey == dataType.Key) {
                item.Outcome = GivingMigrationConstants.Outcomes.Skipped;
                item.Message = "The property already uses this data type";

                return item;
            }

            // Umbraco stores every property value in a column chosen by the data type, so repointing across storage
            // types would orphan the existing values.
            if (property.ValueStorageType != dataType.DatabaseType) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.Message = "The property stores " +
                               property.ValueStorageType +
                               " but the data type stores " +
                               dataType.DatabaseType;

                return item;
            }

            if (preview) {
                item.Outcome = GivingMigrationConstants.Outcomes.NotAttempted;

                return item;
            }

            property.DataTypeId = dataType.Id;
            property.DataTypeKey = dataType.Key;

            _contentTypeService.Save(contentType);

            item.Outcome = GivingMigrationConstants.Outcomes.Rewritten;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "Could not repoint {ContentTypeAlias}.{PropertyAlias}",
                             contentTypeAlias,
                             propertyAlias);

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = ex.Message;
        }

        return item;
    }

    private static void FindReferences(IContent content,
                                       IReadOnlyCollection<Guid> legacyIds,
                                       ICollection<GivingMigrationIssueRes> issues) {
        foreach (var property in content.Properties) {
            var found = new HashSet<Guid>();

            foreach (var value in property.Values) {
                Collect(value.EditedValue as string, legacyIds, found);
                Collect(value.PublishedValue as string, legacyIds, found);
            }

            foreach (var legacyId in found) {
                issues.Add(Issue(GivingMigrationConstants.IssueKinds.ResidualReference,
                                 GivingMigrationConstants.Severities.Blocker,
                                 legacyId,
                                 content.Name,
                                 property.Alias,
                                 "The content still references a legacy form that the purge would delete"));
            }
        }
    }

    private static void Collect(string value, IReadOnlyCollection<Guid> legacyIds, ISet<Guid> found) {
        if (!Matches(value)) {
            return;
        }

        foreach (Match match in AnyDocumentUdi.Matches(value)) {
            if (Guid.TryParseExact(match.Groups[1].Value, "N", out var legacyId) && legacyIds.Contains(legacyId)) {
                found.Add(legacyId);
            }
        }
    }

    // Recycle bin content is returned by the descendant walk but is not live, so it is filtered out rather than
    // being rewritten and reported as an unclearable issue.
    private IEnumerable<IContent> GetAllContent() {
        long page = 0;
        long total;

        do {
            foreach (var content in _contentService.GetPagedDescendants(UmbracoSystem.Root,
                                                                        page,
                                                                        PageSize,
                                                                        out total)) {
                if (!content.Trashed) {
                    yield return content;
                }
            }

            page++;
        } while (page * PageSize < total);
    }

    private GivingMigrationRewriteItemRes RewriteOne(IContent content,
                                                     bool isBlueprint,
                                                     IReadOnlyCollection<string> propertyAliases,
                                                     IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                                     string itemContentTypeAlias,
                                                     Guid dataTypeKey,
                                                     bool preview,
                                                     ICollection<GivingMigrationIssueRes> issues) {
        var references = 0;
        var rewritten = 0;
        var changes = new List<PropertyChange>();

        // Publishing a page also publishes whatever draft its editors had in progress, so a page with pending
        // changes is reported rather than rewritten and the operator decides what to do with the draft.
        var pendingDraft = content.Published && content.Edited;

        foreach (var property in content.Properties) {
            foreach (var value in property.Values) {
                if (!Supported(content, property.PropertyType, value)) {
                    continue;
                }

                var original = value.EditedValue as string;

                if (!Matches(original)) {
                    continue;
                }

                var result = RewriteValue(original,
                                          property.PropertyType,
                                          propertyAliases,
                                          campaigns,
                                          itemContentTypeAlias,
                                          dataTypeKey,
                                          content,
                                          issues);

                references += result.References;
                rewritten += result.Rewritten;

                if (result.Rewritten > 0) {
                    changes.Add(new PropertyChange(property.Alias, value.Culture, value.Segment, result.Json));
                }
            }
        }

        if (references == 0) {
            return null;
        }

        var item = new GivingMigrationRewriteItemRes();
        item.PageId = content.Id;
        item.PageKey = content.Key;
        item.PageName = content.Name;
        item.IsBlueprint = isBlueprint;
        item.PropertyAlias = string.Join(", ", changes.Select(x => x.Alias).Distinct());
        item.References = references;
        item.Rewritten = rewritten;

        if (changes.Count == 0) {
            item.Outcome = GivingMigrationConstants.Outcomes.Skipped;
            item.Message = "No reference could be resolved to a migrated campaign";

            return item;
        }

        if (preview) {
            item.Outcome = GivingMigrationConstants.Outcomes.NotAttempted;

            return item;
        }

        if (pendingDraft) {
            item.Outcome = GivingMigrationConstants.Outcomes.Skipped;
            item.Message = "The page has unpublished changes, so publishing the rewrite would publish them too. " +
                           "Publish or discard the draft and run the rewrite again";

            return item;
        }

        try {
            foreach (var change in changes) {
                content.SetValue(change.Alias, change.Json, change.Culture, change.Segment);
            }

            // A blueprint has no published version and is persisted through its own service method.
            if (isBlueprint) {
                _contentService.SaveBlueprint(content, UmbracoSecurity.SuperUserId);

                item.Outcome = GivingMigrationConstants.Outcomes.Rewritten;
            } else if (content.Published) {
                var published = _contentService.SaveAndPublish(content,
                                                               PublishedCultures(content),
                                                               UmbracoSecurity.SuperUserId);

                if (published.Success) {
                    item.Outcome = GivingMigrationConstants.Outcomes.Rewritten;
                } else {
                    item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                    item.Message = "The page could not be published: " + published.Result;
                }
            } else {
                var saved = _contentService.Save(content, UmbracoSecurity.SuperUserId);

                if (saved.Success) {
                    item.Outcome = GivingMigrationConstants.Outcomes.Rewritten;
                } else {
                    item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                    item.Message = "The page could not be saved: " + saved.Result;
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "There was an error rewriting blocks on page with id {PageId}", content.Key);

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = ex.Message;
        }

        return item;
    }

    private RewriteResult RewriteValue(string json,
                                       IPropertyType propertyType,
                                       IReadOnlyCollection<string> propertyAliases,
                                       IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                       string itemContentTypeAlias,
                                       Guid dataTypeKey,
                                       IContent content,
                                       ICollection<GivingMigrationIssueRes> issues) {
        JToken token;

        try {
            token = JToken.Parse(json);
        } catch (JsonException) {
            return RewriteRawValue(json,
                                   propertyType,
                                   propertyAliases,
                                   campaigns,
                                   itemContentTypeAlias,
                                   dataTypeKey,
                                   content,
                                   issues);
        }

        var references = 0;
        var rewritten = 0;

        foreach (var holder in GetObjects(token)) {
            // Repoint takes an operator supplied list of block types, so a type left out of it would otherwise have
            // its legacy value overwritten with nested content JSON its editor cannot read, destroying the original
            // reference. The binding itself is the authority for what may be written, never the supplied list.
            var holderAlias = holder[NestedContentTypeAlias]?.Value<string>();

            foreach (var alias in propertyAliases) {
                if (holder[alias] != null && !IsBound(holderAlias, alias, dataTypeKey)) {
                    if (Matches(holder[alias].ToString())) {
                        issues.Add(NotRepointed(content, holderAlias, alias));
                    }

                    continue;
                }

                if (holder[alias] is not JValue candidate || candidate.Type != JTokenType.String) {
                    if (Matches(holder[alias]?.ToString())) {
                        issues.Add(Unrecognised(content, alias));
                    }

                    continue;
                }

                var raw = candidate.Value<string>() ?? string.Empty;
                var match = DocumentUdi.Match(raw);

                if (!match.Success || !Guid.TryParseExact(match.Groups[1].Value, "N", out var legacyId)) {
                    if (Matches(raw)) {
                        issues.Add(Unrecognised(content, alias));
                    }

                    continue;
                }

                references++;

                if (!campaigns.TryGetValue(legacyId, out var campaign)) {
                    issues.Add(Issue(GivingMigrationConstants.IssueKinds.UnmappedReference,
                                     GivingMigrationConstants.Severities.Blocker,
                                     legacyId,
                                     content.Name,
                                     alias,
                                     "The legacy form is not in the migration ledger so the reference was left " +
                                     "as it is"));

                    continue;
                }

                holder[alias] = BuildDonationFormValue(itemContentTypeAlias, campaign);

                rewritten++;
            }
        }

        return rewritten == 0
                   ? new RewriteResult(references, 0, null)
                   : new RewriteResult(references, rewritten, token.ToString(Formatting.None));
    }

    private static string BuildDonationFormValue(string itemContentTypeAlias, GivingMigrationLedgerEntry campaign) {
        var elementId = ElementKind.DonationFormCampaign.ToEnumString() + "/" + campaign.NewId.ToString("D");

        var item = new JObject();
        item[NestedKey] = Guid.NewGuid().ToString();
        item[NestedName] = campaign.Name;
        item[NestedContentTypeAlias] = itemContentTypeAlias;
        item[PlatformsConstants.DonationFormItems.Properties.Campaign] =
            JsonConvert.SerializeObject(new[] { elementId });

        return new JArray(item).ToString(Formatting.None);
    }

    // Looking the data type up by name alone is not enough: a site can already own a type with this name built on a
    // different editor, and writing nested content values into it leaves every block silently empty.
    private IDataType FindDonationFormDataType(out string problem) {
        var name = PlatformsSchemaConstants.DataTypes.DonationFormList;
        var dataType = _dataTypeEditor.Find(name);

        if (dataType == null) {
            problem = "The data type " + name.Quote() + " does not exist, so the schema seeder has not run";

            return null;
        }

        if (!dataType.EditorAlias.EqualsInvariant(UmbracoPropertyEditors.Aliases.NestedContent)) {
            problem = "The data type " +
                      name.Quote() +
                      " uses editor " +
                      dataType.EditorAlias.Quote() +
                      " but the donation form picker must use " +
                      UmbracoPropertyEditors.Aliases.NestedContent.Quote();

            return null;
        }

        problem = null;

        return dataType;
    }

    private static IReadOnlyList<JObject> GetObjects(JToken token) {
        var objects = new List<JObject>();

        if (token is JObject root) {
            objects.Add(root);
        }

        if (token is JContainer container) {
            objects.AddRange(container.Descendants().OfType<JObject>());
        }

        return objects;
    }

    // A picker bound straight to a page property stores a bare UDI rather than a JSON document, so the walk above
    // never reaches it.
    private RewriteResult RewriteRawValue(string value,
                                          IPropertyType propertyType,
                                          IReadOnlyCollection<string> propertyAliases,
                                          IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                          string itemContentTypeAlias,
                                          Guid dataTypeKey,
                                          IContent content,
                                          ICollection<GivingMigrationIssueRes> issues) {
        var propertyAlias = propertyType.Alias;

        if (!propertyAliases.Contains(propertyAlias, true)) {
            return RewriteResult.None;
        }

        // The property holds the picker directly, so its own binding decides whether a nested content value may be
        // written over the legacy reference.
        if (propertyType.DataTypeKey != dataTypeKey) {
            issues.Add(NotRepointed(content, content.ContentType.Alias, propertyAlias));

            return RewriteResult.None;
        }

        var match = DocumentUdi.Match(value.Trim());

        if (!match.Success || !Guid.TryParseExact(match.Groups[1].Value, "N", out var legacyId)) {
            issues.Add(Unrecognised(content, propertyAlias));

            return RewriteResult.None;
        }

        if (!campaigns.TryGetValue(legacyId, out var campaign)) {
            issues.Add(Issue(GivingMigrationConstants.IssueKinds.UnmappedReference,
                             GivingMigrationConstants.Severities.Blocker,
                             legacyId,
                             content.Name,
                             propertyAlias,
                             "The legacy form is not in the migration ledger so the reference was left as it is"));

            return new RewriteResult(1, 0, null);
        }

        return new RewriteResult(1, 1, BuildDonationFormValue(itemContentTypeAlias, campaign));
    }

    // A content type can still hold culture scoped rows left over from when it varied, and writing one back is
    // rejected because the effective variation is the intersection of the content type and the property type, so an
    // invariant property on a varying type is as much a problem as a row left on an invariant type.
    private static bool Supported(IContent content, IPropertyType propertyType, IPropertyValue value) {
        var variations = content.ContentType.Variations & propertyType.Variations;

        if (value.Culture.HasValue() && !variations.HasFlag(ContentVariation.Culture)) {
            return false;
        }

        return !value.Segment.HasValue() || variations.HasFlag(ContentVariation.Segment);
    }

    // A page property and a nested content holder both answer the same question - is this property actually bound to
    // the donation form picker - and the lookup is repeated for every page, so the answer is kept.
    private bool IsBound(string contentTypeAlias, string propertyAlias, Guid dataTypeKey) {
        if (!contentTypeAlias.HasValue()) {
            return false;
        }

        var cacheKey = contentTypeAlias + "/" + propertyAlias;

        if (_bindings.TryGetValue(cacheKey, out var bound)) {
            return bound;
        }

        var contentType = _contentTypeService.Get(contentTypeAlias);
        var property = contentType?.PropertyTypes.FirstOrDefault(x => x.Alias.EqualsInvariant(propertyAlias));

        bound = property != null && property.DataTypeKey == dataTypeKey;

        _bindings[cacheKey] = bound;

        return bound;
    }

    private static string[] PublishedCultures(IContent content) {
        // Publishing with the default of every culture would push live any culture an editor had deliberately left
        // unpublished, so only the cultures already published are republished.
        return content.ContentType.Variations.HasFlag(ContentVariation.Culture)
                   ? content.PublishedCultures.ToArray()
                   : ["*"];
    }

    private static bool Matches(string value) {
        return value.HasValue() && value.IndexOf(DocumentUdiPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static GivingMigrationIssueRes NotRepointed(IContent content,
                                                        string contentTypeAlias,
                                                        string propertyAlias) {
        return Issue(GivingMigrationConstants.IssueKinds.NotRepointed,
                     GivingMigrationConstants.Severities.Blocker,
                     Guid.Empty,
                     content.Name,
                     propertyAlias,
                     "The property on " +
                     (contentTypeAlias.HasValue() ? contentTypeAlias : "an unidentified element type").Quote() +
                     " is not bound to the donation form picker, so the reference was left as it is. Repoint that " +
                     "content type and run the rewrite again");
    }

    private static GivingMigrationIssueRes Unrecognised(IContent content, string propertyAlias) {
        return Issue(GivingMigrationConstants.IssueKinds.UnrecognisedReference,
                     GivingMigrationConstants.Severities.Blocker,
                     Guid.Empty,
                     content.Name,
                     propertyAlias,
                     "The picker holds a document reference in a shape the rewriter does not recognise, such as a " +
                     "multiple item value, so it was left as it is");
    }

    private static GivingMigrationIssueRes Issue(string kind,
                                                 string severity,
                                                 Guid legacyId,
                                                 string pageName,
                                                 string propertyAlias,
                                                 string detail) {
        var res = new GivingMigrationIssueRes();
        res.Kind = kind;
        res.Severity = severity;
        res.LegacyId = legacyId;
        res.LegacyName = pageName;
        res.PropertyAlias = propertyAlias;
        res.Detail = detail;

        return res;
    }

    private class PropertyChange {
        public PropertyChange(string alias, string culture, string segment, string json) {
            Alias = alias;
            Culture = culture;
            Segment = segment;
            Json = json;
        }

        public string Alias { get; }
        public string Culture { get; }
        public string Segment { get; }
        public string Json { get; }
    }

    private class RewriteResult {
        public static readonly RewriteResult None = new(0, 0, null);

        public RewriteResult(int references, int rewritten, string json) {
            References = references;
            Rewritten = rewritten;
            Json = json;
        }

        public int References { get; }
        public int Rewritten { get; }
        public string Json { get; }
    }
}
