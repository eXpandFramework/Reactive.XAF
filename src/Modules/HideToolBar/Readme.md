![](http://45-126-125-189.cloud-xip.com/nuget/v/Xpand.XAF.Modules.HideToolBar.svg?&style=flat) ![](http://45-126-125-189.cloud-xip.com/nuget/dt/Xpand.XAF.Modules.HideToolBar.svg?&style=flat)

[![GitHub issues](http://45-126-125-189.cloud-xip.com/github/issues/eXpandFramework/expand/HideToolBar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AHideToolBar) [![GitHub close issues](http://45-126-125-189.cloud-xip.com/github/issues-closed/eXpandFramework/eXpand/HideToolBar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AHideToolBar)
# About 

The `HideToolBar` package uses the the `IModelListView.HideToolBar` attribute to hide the toolbar.

## Details
This is a `platform agnostic` module that satisfies the following conditions:
1. For all `Nested Frames` with `HIdeToolBar` attribute enabled a signal will be created out of the [Frame.TemplatedChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Frame.TemplateChanged) and the [Frame.TemplateViewChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Frame.TemplateViewChanged) events. For each signal emit the the ToolBar will be hidden.
2. For Windows platform in addition to the ToolBar the `DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController` will be disabled which controls the Show Toolbar context menu.

<twitter>
![image](https://user-images.githubusercontent.com/159464/58032948-418c4500-7b2c-11e9-96bc-f853bd4b9287.png)
</twitter>
### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.HideToolBar.HideToolBar). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/Reactive.XAF#compatibility-matrix)
### Examples
The module is integrated with the `ExcelImporter`.

Next screenshot is an example from ExcelImporter from the view that maps the Excel columns with the BO members. On the right side the nested ListView has the HideToolBar set to true.

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.


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
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.3
 |Xpand.Extensions.Reactive|4.222.3
 |Xpand.Extensions.XAF|4.222.3
 |Xpand.Extensions|4.222.3
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.222.3
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Xpand.Patcher|3.0.17
 |Microsoft.CSharp|4.7.0
 |System.Reactive|5.0.0
 |Newtonsoft.Json|13.0.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.3

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.HideToolBar.HideToolBarModule))
```

