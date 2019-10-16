![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.AutoCommit.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.AutoCommit.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/AutoCommit.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+AutoCommit) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/AutoCommit.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+AutoCommit)
# About 

The `AutoCommit` package can be used to enable specific autocommit scenarios by setting the `IModelObjectView.AutoCommit` attribute to true. The implemented autocommit scenarios are described in the details section.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.AutoCommit`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.AutoCommitModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.6
 |System.Reactive|4.2.0
 |System.ValueTuple|4.5.0
 |Xpand.Extensions|0.0.2
 |Xpand.Extensions.XAF|0.0.2
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|1.2.61
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.35

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the contructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.AutoCommit.AutoCommitModule))
```

## Details
The module satisfies the following conditions:
1. When any `ObjectView` with `AutoCommit` enabled a signal will be created out of the [View.Closing](https://documentation.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.View.Closing.event) and the [QueryCanChangeCurrentObject](https://documentation.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.View.QueryCanChangeCurrentObject.event) events. For each signal emit the `View.ObjectSpace.CommitChanges()` is called.
2. When the `ASPxListEditor` of any `ListView` with `AllowEdit` in `BatchEdit` mode and `Autocommit` loses focus (`Client side`) then `View.ObjectSpace.CommitChanges()` is called.

![image](https://user-images.githubusercontent.com/159464/56097334-50fbeb00-5efb-11e9-921b-08f6c2d5b607.png)

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.AutoCommit.AutoCommit). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
### Examples
The module is integrated with the `ExcelImporter`.

Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. Both the DetailView and its nested ListView have AutoCommit set to true.

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)

