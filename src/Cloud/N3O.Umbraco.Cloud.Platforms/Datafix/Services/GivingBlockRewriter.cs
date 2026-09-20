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
            _logger.LogError(ex, "Could not repoint {ContentTypeAlias}.{PropertyAlias}", contentTypeAlias, propertyAlias);

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = ex.Message;
        }

        return item;
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

        if (FindDonationFormDataType(out var problem) == null) {
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

        long page = 0;
        long total;

        do {
            foreach (var content in _contentService.GetPagedDescendants(UmbracoSystem.Root,
                                                                        page,
                                                                        PageSize,
                                                                        out total)) {
                res.PagesScanned++;

                var item = RewriteContent(content,
                                          propertyAliases,
                                          campaigns,
                                          itemContentType.Alias,
                                          req.Preview,
                                          issues);

                if (item != null) {
                    items.Add(item);
                }
            }

            page++;
        } while (page * PageSize < total);

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

    private GivingMigrationRewriteItemRes RewriteContent(IContent content,
                                                         IReadOnlyCollection<string> propertyAliases,
                                                         IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                                         string itemContentTypeAlias,
                                                         bool preview,
                                                         ICollection<GivingMigrationIssueRes> issues) {
        var references = 0;
        var rewritten = 0;
        var changes = new List<PropertyChange>();

        foreach (var property in content.Properties) {
            foreach (var value in property.Values) {
                var original = value.EditedValue as string;

                if (!Matches(original)) {
                    continue;
                }

                var result = RewriteValue(original,
                                          property.Alias,
                                          propertyAliases,
                                          campaigns,
                                          itemContentTypeAlias,
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

        try {
            foreach (var change in changes) {
                content.SetValue(change.Alias, change.Json, change.Culture, change.Segment);
            }

            // A page that is live has to be republished or the published version keeps the legacy reference.
            if (content.Published) {
                var published = _contentService.SaveAndPublish(content, userId: UmbracoSecurity.SuperUserId);

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
                                       string propertyAlias,
                                       IReadOnlyCollection<string> propertyAliases,
                                       IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                       string itemContentTypeAlias,
                                       IContent content,
                                       ICollection<GivingMigrationIssueRes> issues) {
        JToken token;

        try {
            token = JToken.Parse(json);
        } catch (JsonException) {
            return RewriteRawValue(json,
                                   propertyAlias,
                                   propertyAliases,
                                   campaigns,
                                   itemContentTypeAlias,
                                   content,
                                   issues);
        }

        var references = 0;
        var rewritten = 0;

        foreach (var holder in GetObjects(token)) {
            foreach (var alias in propertyAliases) {
                if (holder[alias] is not JValue candidate || candidate.Type != JTokenType.String) {
                    continue;
                }

                var match = DocumentUdi.Match(candidate.Value<string>() ?? string.Empty);

                if (!match.Success || !Guid.TryParseExact(match.Groups[1].Value, "N", out var legacyId)) {
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
                                          string propertyAlias,
                                          IReadOnlyCollection<string> propertyAliases,
                                          IReadOnlyDictionary<Guid, GivingMigrationLedgerEntry> campaigns,
                                          string itemContentTypeAlias,
                                          IContent content,
                                          ICollection<GivingMigrationIssueRes> issues) {
        if (!propertyAliases.Contains(propertyAlias, true)) {
            return RewriteResult.None;
        }

        var match = DocumentUdi.Match(value.Trim());

        if (!match.Success || !Guid.TryParseExact(match.Groups[1].Value, "N", out var legacyId)) {
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

    private static bool Matches(string value) {
        return value.HasValue() && value.IndexOf(DocumentUdiPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
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
