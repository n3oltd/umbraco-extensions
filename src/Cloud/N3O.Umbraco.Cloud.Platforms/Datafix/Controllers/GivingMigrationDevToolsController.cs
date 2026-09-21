using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
[ApiDocument(PlatformsConstants.DevToolsApiName)]
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

    [AllowAnonymous]
    [HttpGet("giving-migration/plan")]
    public ActionResult<GivingMigrationPlanRes> GetPlan() {
        return Ok(_planner.BuildPlan());
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/plan")]
    public ActionResult<GivingMigrationPersistedPlanRes> SavePlan() {
        return Ok(_store.SavePlan(_planner.BuildPlan()));
    }

    [AllowAnonymous]
    [HttpGet("giving-migration/plan/saved")]
    public ActionResult<GivingMigrationPersistedPlanRes> GetSavedPlan() {
        return Ok(_store.GetPlan());
    }

    [AllowAnonymous]
    [HttpDelete("giving-migration/plan")]
    public ActionResult DeletePlan() {
        _store.DeletePlan();

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/migrate")]
    public async Task<ActionResult<GivingMigrationRunRes>> Migrate([FromBody] MigrateGivingReq req,
                                                                   CancellationToken cancellationToken) {
        return Ok(await _runner.MigrateAsync(req ?? new MigrateGivingReq(), cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/complete")]
    public ActionResult<GivingMigrationRunRes> Complete([FromBody] CompleteGivingMigrationReq req) {
        return Ok(_runner.Complete(req ?? new CompleteGivingMigrationReq()));
    }

    [AllowAnonymous]
    [HttpGet("giving-migration/status")]
    public ActionResult<GivingMigrationStatusRes> GetStatus() {
        return Ok(_reporter.BuildStatus());
    }

    [AllowAnonymous]
    [HttpGet("giving-migration/ledger")]
    public ActionResult<GivingMigrationLedgerRes> GetLedger() {
        return Ok(_reporter.BuildLedger());
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/blocks/repoint")]
    public ActionResult<GivingMigrationRepointRes> RepointBlocks([FromBody] RepointGivingBlocksReq req) {
        return Ok(_blockRewriter.Repoint(req ?? new RepointGivingBlocksReq()));
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/blocks/rewrite")]
    public ActionResult<GivingMigrationRewriteRes> RewriteBlocks([FromBody] RewriteGivingBlocksReq req) {
        return Ok(_blockRewriter.Rewrite(req ?? new RewriteGivingBlocksReq()));
    }

    [AllowAnonymous]
    [HttpGet("giving-migration/lock")]
    public ActionResult<GivingMigrationLockRes> GetLock() {
        return Ok(_treeLock.GetStatus());
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/lock")]
    public ActionResult<GivingMigrationLockRes> Lock() {
        return Ok(_treeLock.Lock());
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/unlock")]
    public ActionResult<GivingMigrationLockRes> Unlock() {
        return Ok(_treeLock.Unlock());
    }

    [AllowAnonymous]
    [HttpPost("giving-migration/legacy/purge")]
    public ActionResult<GivingMigrationPurgeRes> Purge([FromBody] PurgeLegacyGivingReq req) {
        return Ok(_purger.Purge(req ?? new PurgeLegacyGivingReq()));
    }
}
