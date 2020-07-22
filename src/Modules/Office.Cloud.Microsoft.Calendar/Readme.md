![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Microsoft.Calendar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Office.Cloud.Microsoft.Calendar) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Microsoft.Calendar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Office.Cloud.Microsoft.Calendar)
# About 

The `Microsoft.Calendar` package integrates with the Office365 Calendar cloud service.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module that authenticates against Azure using the [Xpand.XAF.Modules.Microsoft](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft) package, for details head to it's wiki page.

The `Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar` provides a configurable `two way` synchronization between the `IEvent` Domain Components and the `Microsoft.Graph.Event` entity.
All `CRUD` operations will execute the cloud api after a successful XAF transaction. 

You can use the model to `configure`:

* The subject `Views`, the target container `Calendar` and which Domain Component should be created when a `NewCloudEvent`.</br>
![image](https://user-images.githubusercontent.com/159464/88489382-9cc18f00-cf9c-11ea-9f59-ceb22fbcd16a.png)
* The CRUD `SynchronizationType` and the `CallDirection`.</br>
![image](https://user-images.githubusercontent.com/159464/88489628-01c9b480-cf9e-11ea-95b3-e59e59bcd93b.png)



The package can operate without any configuration by executing a `predefined map` between the `IEvent` and `OutlookEvent` objects on Update and on Insert for both incoming and outgoing calls.

To customize the predefined map you can use a query like the next one which suffix the OutlookEvent subject with the current date:

```cs

CalendarService.CustomizeSynchronization
    .Do(e => {
        var tuple = e.Instance;
        if (tuple.mapAction != MapAction.Delete){
            if (tuple.callDirection == CallDirection.In){
                tuple.local.Subject = $"{tuple.cloud.Subject} - {DateTime.Now}";
            }
            else if (tuple.callDirection == CallDirection.Out){
                tuple.cloud.Subject = $"{tuple.local.Subject} - {DateTime.Now}";
            }
            e.Handled = true;
        }
    })
    .Subscribe();
```
**Cloud to local synchronization:**
The package uses [delta queries](https://docs.microsoft.com/en-us/graph/delta-query-overview) to track changes in Microsoft Graph data.

**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

In the next screencast you can see all `CRUD` operations on the Event BO and how they synchronize with the `Office365` Calendar, for both platforms `Win`, `Web` and both directions `Incoming`, `Outgoing`. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used.

<twitter>

[![Xpand XAF Modules Office Cloud Microsoft Calendar](https://user-images.githubusercontent.com/159464/87413649-3e2b0700-c5d3-11ea-95d1-b44ee2f7891c.gif)
](https://youtu.be/E90hOGf-W2I)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/E90hOGf-W2I)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar`.

    The above only references the dependencies and next steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.CalendarModule));
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
 |**DevExpress.ExpressApp.Xpo**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |Microsoft.Graph.Beta|0.18.0-preview
 |Microsoft.Graph.Core|1.19.0
 |Microsoft.Identity.Client|4.13.0
 |Newtonsoft.Json|12.0.3
 |System.Reactive|4.4.1
 |Xpand.Extensions|2.202.38
 |Xpand.Extensions.Office.Cloud|2.202.38
 |Xpand.Extensions.Reactive|2.202.39
 |Xpand.Extensions.XAF|2.202.39
 |Xpand.Extensions.XAF.Xpo|2.202.35
 |[Xpand.XAF.Modules.Office.Cloud.Microsoft](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Office.Cloud.Microsoft)|2.202.40
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar.Office.Office.Cloud.Microsoft.CalendarModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Microsoft.Calendar.Office.Office.Cloud.Microsoft.Calendar). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

