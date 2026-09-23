# N3O.Umbraco.Monitoring.Sentry

Reports errors and traces to Sentry, and attaches everything the registered log enrichers supply as
tags and context on each event.

Sentry is initialised only in the production and staging environments, so nothing is sent from
development however the configuration is set. Serilog reports at error level and above; tracing is
added to the pipeline in production only.

```json
{
  "Sentry": {
    "Dsn": "<from the secret store>",
    "TracesSampleRate": 0.1,
    "TracesIgnorePaths": [ "/health", "/live", "/ready", "/metrics" ]
  }
}
```

`TracesSampleRate` defaults to nought, which means no traces at all; errors are unaffected by it.
Paths listed in `TracesIgnorePaths` are sampled out entirely, which is what keeps health probes from
dominating the trace volume — the match is a case-insensitive substring of the transaction name, so
a short entry excludes more than it looks like it does.

Events are also rate limited before sending: beyond a number of events with the same fingerprint
within a window, the rest are dropped, and the last one sent before the limit is tagged so the gap
is visible in Sentry rather than silent.
