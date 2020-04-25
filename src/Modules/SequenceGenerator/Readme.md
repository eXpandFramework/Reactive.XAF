![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.SequenceGenerator.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.SequenceGenerator.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/SequenceGenerator.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+SequenceGenerator) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/SequenceGenerator.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+SequenceGenerator)
# About 

The `SequenceGenerator` updates Bushiness Objects members with unique sequential values.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.SequenceGenerator`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.SequenceGeneratorModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp**|**Any**
 |**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2019.1.3
 |System.Reactive|4.3.2
 |Xpand.Extensions|2.201.14.1
 |Xpand.Extensions.Reactive|2.201.14.1
 |Xpand.Extensions.XAF|2.201.14.2
 |Xpand.Extensions.XAF.Xpo|2.201.13.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.201.14.2
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.201.6

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.SequenceGenerator.SequenceGeneratorModule))
```

## Details
The `SequenceGenerator` module is a well tested implementation variation of the E3459. 

In details: when any XAF database transaction starts an Exclicit UnitOfWork is used to acquire a lock to the SequenceStorage table. If the table is already locked the it retries forever, if not it queries the SequenceStorage table for all the object types that match the objects inside the transaction and assigns their binding members (e.g. a long SequenceNumber member). After the XAF transaction completes with success or with a failure the database lock is released.

##### <u>Configuration</u>
You can configure the Sequence binding at runtime by creating instances of the SequenceStorage BO as shown in the next screencast.

Because the SequenceStorage table is a normal XAF BO, it is possible to create sequence bindings in code by creating instances of that object. However we do not recommend creating instances directly but use the provided API (possibly in a ModuleUpdater) which respects additional constrains and validations.

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/SequenceGenerator)

