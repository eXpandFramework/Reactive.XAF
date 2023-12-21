![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.GridListEditor.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.GridListEditor.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/GridListEditor.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AGridListEditor) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/GridListEditor.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AGridListEditor)
# About 

The `GridListEditor` module hosts features related to XAF GridListEditor. 

## Details

This is a `WinForms module` which can be configured using model rules as shown .

<twitter>

![image](https://user-images.githubusercontent.com/159464/126141781-32096b48-6b88-4109-9431-8800909a5576.png)

</twitter>

**Rules:**
1. `RememberTopRow`: To freeze the TopRowIndex of a ListView when its CollectionSource get reloaded add a new rule to your model as in image above. The rule is valuable in scenarios where an external signal notifies about new data and you refresh your View. Such an example is the [Reactive.Logger.Client](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win)

2. `FocusRow`: Configure which gridview row handle will get focused once a ListView is shown. 


--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.GridListEditor`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.GridListEditorModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.1
 |Xpand.Extensions.Reactive|4.232.1
 |Xpand.Extensions|4.232.1
 |Xpand.Extensions.XAF|4.232.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.232.1
 |System.Reactive|6.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|7.0.2
 |System.Threading.Tasks.Dataflow|7.0.0
 |Xpand.Patcher|3.0.22
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.GridListEditor.GridListEditorModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.GridListEditor.GridListEditor). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/Reactive.XAF#compatibility-matrix)
### Examples

