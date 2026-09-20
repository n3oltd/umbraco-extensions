using N3O.Umbraco.Cloud;
using N3O.Umbraco.Cloud.Platforms.Models;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoSecurity = Umbraco.Cms.Core.Constants.Security;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyGivingPurger : ILegacyGivingPurger {
    private const int PageSize = 200;

    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationReporter _reporter;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public LegacyGivingPurger(IGivingMigrationPlanner planner,
                              IGivingMigrationReporter reporter,
                              IContentService contentService,
                              IContentTypeService contentTypeService,
                              ISubscriptionAccessor subscriptionAccessor) {
        _planner = planner;
        _reporter = reporter;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public GivingMigrationPurgeRes Purge(PurgeLegacyGivingReq req) {
        var res = new GivingMigrationPurgeRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();
        res.Permanent = req.Permanent;

        var plan = _planner.BuildPlan();
        res.LegacyForms = plan.Summary.LegacyForms;

        if (!_reporter.BuildStatus().Complete) {
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

        var items = new List<GivingMigrationPurgeItemRes>();

        // Deleting a folder or form takes its whole subtree with it, so only the top level is walked.
        foreach (var root in roots) {
            foreach (var child in GetChildren(root.Id)) {
                items.Add(Purge(child, req.Permanent));
            }
        }

        res.Expected = items.Count;
        res.Items = items;
        res.Purged = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Purged);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);

        return res;
    }

    private GivingMigrationPurgeItemRes Purge(IContent content, bool permanent) {
        var item = new GivingMigrationPurgeItemRes();
        item.LegacyId = content.Key;
        item.LegacyPath = content.Name;
        item.ContentTypeAlias = content.ContentType.Alias;

        var result = permanent
                         ? _contentService.Delete(content, UmbracoSecurity.SuperUserId)
                         : _contentService.MoveToRecycleBin(content, UmbracoSecurity.SuperUserId);

        if (result.Success) {
            item.Outcome = GivingMigrationConstants.Outcomes.Purged;
        } else {
            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = result.Result.ToString();
        }

        return item;
    }

    private IReadOnlyList<IContent> GetLegacyRoots() {
        var contentType = _contentTypeService.Get(GivingMigrationConstants.Legacy.DonationFormsAlias);

        if (contentType == null) {
            return [];
        }

        var roots = new List<IContent>();
        long page = 0;
        long total;

        do {
            var items = _contentService.GetPagedOfType(contentType.Id, page, PageSize, out total, null);

            roots.AddRange(items.Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return roots;
    }

    private IReadOnlyList<IContent> GetChildren(int parentId) {
        var children = new List<IContent>();
        long page = 0;
        long total;

        do {
            children.AddRange(_contentService.GetPagedChildren(parentId, page, PageSize, out total)
                                             .Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return children;
    }
}
