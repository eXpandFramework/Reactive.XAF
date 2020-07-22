![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Logger.Client.Win.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.Client.Win.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Reactive.Logger.Client.Win.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive.Logger.Client.Win) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Reactive.Logger.Client.Win.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive.Logger.Client.Win)
# About 

The `Reactive.Logger.Client.Win` is an executable able connect to any XAF app that uses the `Reactive.Logger.Hub`.

## Details
The client is an executable XAF Windows Forms application and not a XAF module, which at the moment is distributed from Nuget.org but in future will use Chocolatey.org as it makes more sense. So you download the package and run the exe which will monitor the ports configured in your model. By default it monitors the range 
![image](https://user-images.githubusercontent.com/159464/66378578-62124a00-e9bc-11e9-9a8c-479c4c6e4037.png)

If a XAF application that has the [Reactive.Logger.Hub](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Hub) found transmitting in this range it will connect and persist all message that describe the remote application pipeline flow. 

Bellow is a screencast when running the client and execute one of our tests. The `Bind_Only_NullAble_Properties_That_are_not_Null` test makes sure that the `ModelMapper` module will only bind those model properties that have value.

<twitter>

[![2019-09-21_23-53-04](https://user-images.githubusercontent.com/159464/65379147-1da06200-dccc-11e9-93c4-98c3a21ea3ed.gif)](https://www.youtube.com/watch?v=XPSH637vwUU)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://www.youtube.com/watch?v=XPSH637vwUU)

The information can be overwhelming however simple filters, like the one applied in the previous screencast, let us extract very useful data as to what the ModelMapper did. 

The client is a Hybrid logger and and uses both the Logger and the Hub so everything discussed there applies also here.

--- 

**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples
Head to [Reactive.Logger](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive.Logger)

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Reactive.Logger.Client.Win`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Reactive.Logger.Client.WinModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
 |**DevExpress.ExpressApp.Win**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |MagicOnion|2.6.3
 |System.Reactive|4.4.1
 |[Xpand.XAF.Modules.GridListEditor](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.GridListEditor)|2.202.39
 |[Xpand.XAF.Modules.OneView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.OneView)|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.XAF.Modules.Reactive.Logger.Hub](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive.Logger.Hub)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Reactive.Logger.ReactiveLoggerModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Reactive.Logger.ReactiveLogger). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

