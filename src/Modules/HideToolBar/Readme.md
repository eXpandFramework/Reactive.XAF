![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.HideToolBar.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.HideToolBar.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/HideToolBar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+HideToolBar) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/HideToolBar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+HideToolBar)
# About 

The `HIdeToolBar` package can be used to hide the Nested ListView toolbar. The module extends the model with the `IModelListView.HideToolBar` attribute. The implemented scenarios are described in the details section.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.HIdeToolBar`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.HIdeToolBarModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: v4.6.1`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.26
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|1.2.31
 |fasterflect|2.1.3
 |System.ValueTuple|4.5.0

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call when [XafApplication.SetupComplete](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.XafApplication.SetupComplete).
```ps1
((Xpand.XAF.Modules.HideToolBarModule) Application.Modules.FindModule(typeof(Xpand.XAF.Modules.HideToolBarModule))).Unload();
```
## Details
The module satisfies the following conditions:
1. For all `Nested Frames` with `HIdeToolBar` attribute enabled a signal will be created out of the [Frame.TemplatedChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Frame.TemplateChanged) and the [Frame.TemplateViewChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Frame.TemplateViewChanged) events. For each signal emit the the ToolBar will be hidden.
2. For Windows platform in addition to the ToolBar the `DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController` will be disabled which controls the Show Toolbar context menu.

![image](https://user-images.githubusercontent.com/159464/58032948-418c4500-7b2c-11e9-96bc-f853bd4b9287.png)

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Modules/HIdeToolBar)

### Examples
The module is integrated with the `ExcelImporter`.

Next screenshot is an example from ExcelImporter from the view that maps the Excel columns with the BO members. On the right side the nested ListView has the HideToolBar set to true.

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)
