# N3O.Umbraco.Monitoring

Collects the context that should be attached to every log entry and error report. An `ILogEnricher`
contributes tags and context data and is discovered automatically across our assemblies, so a
package adds what identifies it without the monitoring package knowing about it.

This package only gathers that context; a monitoring package decides where it goes.
