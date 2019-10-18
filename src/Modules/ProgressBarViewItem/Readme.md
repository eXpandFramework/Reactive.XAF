![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ProgressBarViewItem.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ProgressBarViewItem.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/ProgressBarViewItem.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+ProgressBarViewItem) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/ProgressBarViewItem.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+ProgressBarViewItem)
# About 

The `ProgressBarViewItem` package contains a ViewItem that can help you display a progress for your long running tasks. Examples in the details section of this page.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ProgressBarViewItem`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ProgressBarViewItemModule));
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
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|1.2.63
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.35

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the contructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ProgressBarViewItem.ProgressBarViewItemModule))
```

## Details
The module registers a `ProgressViewItem` into the [ViewItems](https://docs.devexpress.com/eXpressAppFramework/112612/concepts/ui-construction/view-items) of your application.
![image](https://user-images.githubusercontent.com/159464/58765488-56c99080-857c-11e9-8522-4c716df019fd.png).

This is a ViewItem and is not bound to a property. To add it in a DetailView Layout you have first to add it in the IModelViewItems collection.


Let's assume you have a long running task such as an import which is done in a different thread and an asynchronous sequence emits its progress. To bind the sequence to a DetailView ProgressViewItem we have to: 

```cs
var progressBarViewItem = View.GetItems<ProgressBarViewItemBase>().First();
progressBarViewItem.Start();
```
The `ProgressViewItem` implements the `IObserver<decimal>` so after start the binding becomes as simple as:

```cs
await signal.Cast<Decimal>.Do(progressBarViewItem);
```

The `ProgressBarViewItem` features completion notification which can be configured like:
```cs
progressBarViewItem.SetFinishOptions(new MessageOptions(){});
```
![image](https://user-images.githubusercontent.com/159464/58765606-5da4d300-857d-11e9-83ba-79c8f1bf6463.png)


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.ProgressBarViewItem.ProgressBarViewItem). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
### Examples
The module is integrated with the `ExcelImporter`.

Next we see how it looks on Desktop:

![win](https://user-images.githubusercontent.com/159464/58791920-ce8ad000-85fb-11e9-8a00-bd72e891c8b7.gif)

and the same view on the web:

![web2](https://user-images.githubusercontent.com/159464/58791676-53291e80-85fb-11e9-81de-6ed7db651219.gif)

