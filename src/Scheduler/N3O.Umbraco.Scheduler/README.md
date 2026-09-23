# N3O.Umbraco.Scheduler

Runs work in the background on Hangfire. A job is a request handled through the mediator, so the
same command runs identically whether it is invoked inline or enqueued, and `IBackgroundJob`
enqueues one immediately or schedules it for an instant or a delay.

Everything here depends on the Umbraco connection string, which is where Hangfire stores its jobs.
With no connection string nothing is registered at all — no servers, no dashboard — and enqueuing
will fail rather than run inline.

There are two queues, default and long jobs, each served by its own Hangfire server with its own
worker count. Putting a slow job on the long jobs queue is what keeps it from occupying the workers
that short jobs need.

A recurring job declares itself with `[RecurringJob]`, giving a name and a cron expression, and is
registered at startup; recurring jobs that no longer exist in code are removed, so a renamed job
does not leave its old schedule running. A command marked `[RunsWhereQueued]` must execute on the
instance that enqueued it: when a worker on another instance picks it up it is proxied over HTTP to
the originating one, identified by machine name and version, rather than run locally.

The Hangfire dashboard is mounted inside the back office and requires access to the settings
section.

```json
{
  "N3O": {
    "Scheduler": {
      "DefaultWorkerCount": 1,
      "LongJobsWorkerCount": 1,
      "JobTimeoutMinutes": 30
    }
  }
}
```

Both worker counts default to one, so jobs run one at a time per queue until they are raised; raise
them past what the database can take and the contention moves into SQL Server.
