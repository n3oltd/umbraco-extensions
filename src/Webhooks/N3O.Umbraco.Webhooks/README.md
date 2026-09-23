# N3O.Umbraco.Webhooks

Sends webhooks out and receives them in.

Outgoing hooks are queued rather than sent inline: `IWebhooks.Queue` records the event and its body,
and dispatch happens as a background job, so a slow or failing endpoint does not hold up the request
that caused it and a failure is retried by the scheduler. The destination URLs come from Umbraco
settings content, which holds two lists: the production list is used in the production environment
and the staging list in every other, so no non-production site can post to the live endpoints.

Incoming hooks are handled by an `IWebhookReceiver` carrying `[WebhookReceiver]` with the hook
identifier it answers to, discovered automatically; the identifier in the route decides which one
runs.

An `IWebhookTransform` can rewrite an outgoing body before it is sent, claiming the bodies it
applies to by inspecting them; transforms are applied in attribute order and more than one can apply
to the same body. A dispatch that does not return success throws, which is what puts the job back in
the scheduler's hands rather than marking it delivered. Queuing itself never throws — a failure to
queue is logged and swallowed, so the caller's own work is not lost along with the hook.
