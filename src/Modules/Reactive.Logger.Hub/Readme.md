![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Logger.Hub.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.Hub.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Reactive.Logger.Hub.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive.Logger.Hub) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Reactive.Logger.Hub.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive.Logger.Hub)
# About 

The `Reactive.Logger.Hub` transmits or receives the execution pipeLine flow.

## Details

This is a `platform agnostic` module.

1. Client Mode
To install it as a client the XafApplication descendant should implement the `ILoggerHubClientApplication`. Having so the application consult the model as to which ports should listen.

<twitter>

![image](https://user-images.githubusercontent.com/159464/65379322-d23b8300-dcce-11e9-9c43-194b8f6c92c9.png)

</twitter>

Once a TCP Listener found in any of these ports the application will try to receive and persist all transmitted messages. There is already a [Reactive.Logger.Client.exe](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) you can use to monitor all reactive packages of this repository.
2. Server Mode
In this mode the application starts transmitting all messages. It transmits to a preconfigured port to all connected clients. You do not need to implement the `ILoggerHubClientApplication` as before just install the package as a regular XAF module.
![image](https://user-images.githubusercontent.com/159464/65379394-e2079700-dccf-11e9-840d-44ec34849229.png)
The default port is the 61456 for all modules.

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

Head to [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win), [Reactive.Logger](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive.Logger)

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Reactive.Logger.Hub`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Reactive.Logger.HubModule));
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
 |**DevExpress.Xpo**|**Any**
|Fasterflect.Xpand|2.0.7
 |Grpc|2.23.0
 |Grpc.Core|2.25.0
 |Jetbrains.Annotations|2020.1.0
 |MagicOnion|2.6.3
 |System.Interactive.Async|4.1.1
 |System.Reactive|4.4.1
 |Xpand.Extensions.Reactive|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.XAF.Modules.Reactive.Logger](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive.Logger)|2.202.39
 |YamlDotNet|8.1.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Reactive.Logger.Hub.ReactiveLoggerHubModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Reactive.Logger.Hub.ReactiveLoggerHub). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

