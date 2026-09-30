# N3O.Umbraco.Cloud.Platforms.Marketing

Adds one Umbraco Engage segment rule that is satisfied while a telethon campaign is on air, so
editors can target personalisation at the window a telethon is actually running rather than at a
fixed date range.

The rule does not read Umbraco content. It downloads the campaigns the cloud has published for the
subscription and is satisfied when any telethon campaign's broadcast window contains the current
local time, so the segment follows the campaign as published centrally. That also means it is
evaluated against the published file rather than against unsaved or unpublished edits.
