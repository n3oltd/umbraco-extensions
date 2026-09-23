# N3O.Umbraco.Telemetry

Emits OpenTelemetry traces from a site. Activity sources whose names begin with `N3O.` are collected
along with ASP.NET Core instrumentation, so the packages in this repository are traced without a
site naming them. `ITelemetryStopwatch` and the timed activity helpers make a span out of a block of
work, and notification handlers are traced through a decorator.

```json
{
  "Telemetry": {
    "Enabled": true,
    "ServiceName": "<how this site appears in the trace backend>",
    "OtlpExporterUrl": "http://<collector>:4317",
    "UseConsoleExporter": false,
    "CustomActivitySources": [ "MyCompany.*" ]
  }
}
```

Nothing is registered unless `Enabled` is true, so the rest of the section has no effect on its own.
With no `OtlpExporterUrl`, tracing runs and is collected but exported nowhere, which looks like
working instrumentation producing no data. `UseConsoleExporter` writes spans to standard output and
belongs in development only.
