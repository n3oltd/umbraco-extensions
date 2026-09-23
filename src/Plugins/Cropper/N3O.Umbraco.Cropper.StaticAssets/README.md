# N3O.Umbraco.Cropper.StaticAssets

Ships the back office editor for the cropper property, including the crop definitions editor shown
on the data type, as an App_Plugins bundle with a targets file that copies it into the consuming
site.

It contains no C#. `N3O.Umbraco.Cropper` provides the server side and does the cropping; without
this package the crop definitions cannot be edited and no image can be uploaded.
