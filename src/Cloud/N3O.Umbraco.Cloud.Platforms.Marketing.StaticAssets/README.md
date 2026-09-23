# N3O.Umbraco.Cloud.Platforms.Marketing.StaticAssets

Ships the back office editor and display views for the telethon on-air segment rule, as an
App_Plugins bundle with a targets file that copies it into the consuming site.

It contains no C#. The rule itself is in `N3O.Umbraco.Cloud.Platforms.Marketing`; without this
package the rule works but has no interface in the segment editor.
