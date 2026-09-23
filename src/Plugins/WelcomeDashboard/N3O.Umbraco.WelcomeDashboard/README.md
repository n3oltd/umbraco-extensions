# N3O.Umbraco.WelcomeDashboard

Registers a dashboard on the content section of the back office, weighted to appear before Umbraco's
own, and restricted to users who have access to that section.

The dashboard's view comes from `N3O.Umbraco.WelcomeDashboard.StaticAssets`; this package only
declares it, so installing this one alone leaves a dashboard whose view cannot be found.
