using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Authorization;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

// These endpoints migrate and permanently delete content across the whole site, so backoffice access alone is not
// enough: they are restricted to the users who can reach the settings section.
// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
[ApiDocument(PlatformsConstants.DevToolsApiName)]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class GivingMigrationDevToolsController : BackofficeAuthorizedApiController {
    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationRunner _runner;
    private readonly IGivingMigrationReporter _reporter;
    private readonly IGivingMigrationStore _store;
    private readonly IGivingBlockRewriter _blockRewriter;
    private readonly ILegacyGivingTreeLock _treeLock;
    private readonly ILegacyGivingPurger _purger;

    public GivingMigrationDevToolsController(IGivingMigrationPlanner planner,
                                             IGivingMigrationRunner runner,
                                             IGivingMigrationReporter reporter,
                                             IGivingMigrationStore store,
                                             IGivingBlockRewriter blockRewriter,
                                             ILegacyGivingTreeLock treeLock,
                                             ILegacyGivingPurger purger) {
        _planner = planner;
        _runner = runner;
        _reporter = reporter;
        _store = store;
        _blockRewriter = blockRewriter;
        _treeLock = treeLock;
        _purger = purger;
    }

    [HttpGet("giving-migration/plan")]
    public ActionResult<GivingMigrationPlanRes> GetPlan() {
        return Ok(_planner.BuildPlan());
    }

    [HttpPost("giving-migration/plan")]
    public ActionResult<GivingMigrationPersistedPlanRes> SavePlan() {
        return Ok(_store.SavePlan(_planner.BuildPlan()));
    }

    [HttpGet("giving-migration/plan/saved")]
    public ActionResult<GivingMigrationPersistedPlanRes> GetSavedPlan() {
        return Ok(_store.GetPlan());
    }

    [HttpDelete("giving-migration/plan")]
    public ActionResult DeletePlan() {
        _store.DeletePlan();

        return Ok();
    }

    [HttpPost("giving-migration/migrate")]
    public async Task<ActionResult<GivingMigrationRunRes>> Migrate([FromBody] MigrateGivingReq req,
                                                                   CancellationToken cancellationToken) {
        return Ok(await _runner.MigrateAsync(req ?? new MigrateGivingReq(), cancellationToken));
    }

    [HttpPost("giving-migration/complete")]
    public ActionResult<GivingMigrationRunRes> Complete([FromBody] CompleteGivingMigrationReq req) {
        return Ok(_runner.Complete(req ?? new CompleteGivingMigrationReq()));
    }

    [HttpGet("giving-migration/status")]
    public ActionResult<GivingMigrationStatusRes> GetStatus() {
        return Ok(_reporter.BuildStatus());
    }

    [HttpGet("giving-migration/ledger")]
    public ActionResult<GivingMigrationLedgerRes> GetLedger() {
        return Ok(_reporter.BuildLedger());
    }

    [HttpPost("giving-migration/blocks/repoint")]
    public ActionResult<GivingMigrationRepointRes> RepointBlocks([FromBody] RepointGivingBlocksReq req) {
        return Ok(_blockRewriter.Repoint(req ?? new RepointGivingBlocksReq()));
    }

    [HttpPost("giving-migration/blocks/rewrite")]
    public ActionResult<GivingMigrationRewriteRes> RewriteBlocks([FromBody] RewriteGivingBlocksReq req) {
        return Ok(_blockRewriter.Rewrite(req ?? new RewriteGivingBlocksReq()));
    }

    [HttpGet("giving-migration/lock")]
    public ActionResult<GivingMigrationLockRes> GetLock() {
        return Ok(_treeLock.GetStatus());
    }

    [HttpPost("giving-migration/lock")]
    public ActionResult<GivingMigrationLockRes> Lock() {
        return Ok(_treeLock.Lock());
    }

    [HttpPost("giving-migration/unlock")]
    public ActionResult<GivingMigrationLockRes> Unlock() {
        return Ok(_treeLock.Unlock());
    }

    [HttpPost("giving-migration/legacy/purge")]
    public ActionResult<GivingMigrationPurgeRes> Purge([FromBody] PurgeLegacyGivingReq req) {
        return Ok(_purger.Purge(req ?? new PurgeLegacyGivingReq()));
    }
}
