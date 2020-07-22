![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ViewEditMode.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ViewEditMode.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/ViewEditMode.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+ViewEditMode) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/ViewEditMode.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+ViewEditMode)
# About 

The `ViewEditMode` module controls the DetailView.ViewEditMode.


## Details
This is a `platform agnostic` module extends the `IModelDetailView` interface with the `IModelDetailViewViewEditMode`. 

<twitter>

![image](https://user-images.githubusercontent.com/159464/55380067-b7f6c880-5527-11e9-96a1-053fd44095e7.png)

</twitter>

The module uses the next two strategies:

1. It monitors the `DetailView` creation and modifies its ViewEditMode property according to model configuration. However later ViewEditMode property modifications are allowed.
2. It monitors the `ViewEditMode` modification and cancels it if the `LockViewEditMode` attribute is used.

--- 

**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

The module is valuable in scenarios similar to:

1. When you want to `navigate` from a `ListView` to a `DetailView` without the intermediate view which is set to View ViewEditMode.
2. When you develop a `master-detail` layout and you want to control the ViewEditMode state of your

`XtraDashboardModule` ,`ExcelImporterModule` are modules that use the `ViewEditModeModule`.  

Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. 

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ViewEditMode`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ViewEditModeModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |System.Reactive|4.4.1
 |Xpand.Extensions.Reactive|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ViewEditMode.ViewEditModeModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.ViewEditMode.ViewEditMode). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

