# N3O.Umbraco.Cloud.Platforms.StaticAssets

Ships the back office interface for platform content: a preview app that shows how a campaign or
offering will render, and the editor for linking a crowdfunding campaign to its campaign.

Unlike most of the `*.StaticAssets` packages this one carries a little C#, the content app that
places the preview tab. Everything else is App_Plugins assets with a targets file that copies them
into the consuming site.
