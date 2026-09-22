using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoSecurity = Umbraco.Cms.Core.Constants.Security;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyGivingPurger : ILegacyGivingPurger {
    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationReporter _reporter;
    private readonly IGivingBlockRewriter _blockRewriter;
    private readonly ILegacyGivingTreeReader _reader;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly ILogger<LegacyGivingPurger> _logger;
    private IReadOnlyCollection<string> _legacyAliases;

    public LegacyGivingPurger(IGivingMigrationPlanner planner,
                              IGivingMigrationReporter reporter,
                              IGivingBlockRewriter blockRewriter,
                              ILegacyGivingTreeReader reader,
                              IContentService contentService,
                              IContentTypeService contentTypeService,
                              ISubscriptionAccessor subscriptionAccessor,
                              ILogger<LegacyGivingPurger> logger) {
        _planner = planner;
        _reporter = reporter;
        _blockRewriter = blockRewriter;
        _reader = reader;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _subscriptionAccessor = subscriptionAccessor;
        _logger = logger;
    }

    public GivingMigrationPurgeRes Purge(PurgeLegacyGivingReq req) {
        var res = new GivingMigrationPurgeRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();
        res.Permanent = req.Permanent;

        var plan = _planner.BuildPlan();
        res.LegacyForms = plan.Summary.LegacyForms;

        if (!_reporter.BuildStatus(plan).Complete) {
            res.Message = "The legacy tree can only be purged once every campaign has been migrated and every " +
                          "offering published";

            return res;
        }

        if (req.ConfirmFormCount != plan.Summary.LegacyForms) {
            res.Message = "confirmFormCount must equal the " + plan.Summary.LegacyForms +
                          " legacy forms that will be removed";

            return res;
        }

        var roots = GetLegacyRoots();

        if (roots.Count == 0) {
            res.Message = "No legacy giving tree was found";

            return res;
        }

        // Deleting a folder or form takes its whole subtree with it, so the top level is what is walked but the
        // whole subtree is what is destroyed.
        var topLevel = roots.SelectMany(x => GivingMigrationContent.GetChildren(_contentService, x.Id)).ToList();

        var doomed = roots.SelectMany(x => GivingMigrationContent.GetDescendants(_contentService, x.Id)).ToList();
        var doomedKeys = doomed.Select(x => x.Key).ToHashSet();

        // The confirmed count covers the forms, but the delete takes whole subtrees, so anything an editor parked
        // in the tree goes with them without ever having been counted or shown.
        var collateral = doomed.Where(x => !IsLegacyContentType(x.ContentTypeId)).ToList();

        if (collateral.Count > 0) {
            res.Message = "The tree holds " +
                          collateral.Count +
                          " nodes that are not legacy giving content and are not covered by the confirmed count, " +
                          "but would be deleted with it, starting with " +
                          collateral[0].Name.Quote() +
                          ". Move them out of the legacy tree first";

            return res;
        }

        // Forms are discovered by content type anywhere in the tree but only the ones under a legacy root are
        // deleted, so a form the confirmed count included but the delete cannot reach stops the call rather than
        // being silently left behind.
        var unreachable = _reader.GetForms().Where(x => !doomedKeys.Contains(x.Content.Key)).ToList();

        if (unreachable.Count > 0) {
            res.Message = "The confirmed count includes " +
                          unreachable.Count +
                          " legacy forms that are not beneath a " +
                          GivingMigrationConstants.Legacy.DonationFormsAlias.Quote() +
                          " root and so would not be removed, starting with " +
                          unreachable[0].Content.Name.Quote() +
                          ". Move them under a legacy root or remove them by hand first";

            return res;
        }

        // Campaign and offering counts say nothing about whether the rewrite ran, so nothing is deleted until no
        // content references any node in the subtree, not merely any form.
        var residual = _blockRewriter.FindReferences(doomedKeys);

        if (residual.Count > 0) {
            res.Issues = residual;
            res.Message = "There are still " +
                          residual.Count +
                          " references to the legacy tree, so nothing was purged. Run the rewrite and resolve the " +
                          "reported issues first";

            return res;
        }

        var items = new List<GivingMigrationPurgeItemRes>();

        res.Expected = topLevel.Count;

        try {
            foreach (var child in topLevel) {
                items.Add(Purge(child, req.Permanent));
            }
        } catch (Exception ex) {
            // The delete is irreversible, so what was already destroyed is reported even when the call cannot run
            // to the end.
            _logger.LogError(ex, "There was an error purging the legacy giving tree");

            res.Message = "The purge stopped after " + items.Count + " of " + topLevel.Count + ": " + ex.Message;
        }

        res.Items = items;
        res.Purged = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Purged);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);

        return res;
    }

    private GivingMigrationPurgeItemRes Purge(IContent content, bool permanent) {
        var item = new GivingMigrationPurgeItemRes();
        item.LegacyId = content.Key;
        item.LegacyPath = _reader.BuildPath(content);
        item.ContentTypeAlias = content.ContentType.Alias;

        // One node failing to delete must not lose the record of the nodes already destroyed, which is irreversible.
        try {
            var result = permanent
                             ? _contentService.Delete(content, UmbracoSecurity.SuperUserId)
                             : _contentService.MoveToRecycleBin(content, UmbracoSecurity.SuperUserId);

            if (result.Success) {
                item.Outcome = GivingMigrationConstants.Outcomes.Purged;
            } else {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.Message = result.Result.ToString();
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "There was an error purging legacy node with id {LegacyId}", content.Key);

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = ex.Message;
        }

        return item;
    }

    private bool IsLegacyContentType(int contentTypeId) {
        _legacyAliases ??= GivingMigrationContent.GetLegacyAliases(_reader);

        var contentType = _contentTypeService.Get(contentTypeId);

        return contentType != null && _legacyAliases.Contains(contentType.Alias);
    }

    private IReadOnlyList<IContent> GetLegacyRoots() {
        return GivingMigrationContent.GetAllOfAlias(_contentService,
                                                    _contentTypeService,
                                                    GivingMigrationConstants.Legacy.DonationFormsAlias)
                                     .Where(x => !x.Trashed)
                                     .ToList();
    }
}
