![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Google.Calendar.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Google.Calendar.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Google.Calendar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.Cloud.Google.Calendar) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Google.Calendar.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.Cloud.Google.Calendar)
# About 

The `Google.Calendar` package integrates with the Google Calendar cloud service.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module that authenticates against Google using the [Xpand.XAF.Modules.Google](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Google) package, for details head to it's wiki page.

The `Xpand.XAF.Modules.Office.Cloud.Google.Calendar` provides a configurable `two way` synchronization between the `IEvent` Domain Components and the `Google.Apis.Calendar.v3.Event` entity.
All `CRUD` operations will execute the cloud api after a successful XAF transaction. 

You can use the model to `configure`:

* The subject `Views`, the target container `Calendar` and which Domain Component should be created when a `NewCloudEvent`.</br>
![image](https://user-images.githubusercontent.com/159464/93872067-48a30480-fcd8-11ea-92c7-3512999e53e9.png)
* The CRUD `SynchronizationType` and the `CallDirection`.</br>
![image](https://user-images.githubusercontent.com/159464/93872150-6a03f080-fcd8-11ea-92b0-2289b38032d4.png)



The package can operate without any configuration by executing a `predefined map` between the `IEvent` and `Google.Apis.Calendar.v3.Event` objects on Update and on Insert for both incoming and outgoing calls.

To customize the predefined map you can use a query like the next one which suffix the Google.Apis.Calendar.v3.Event subject with the current date:

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
The package track changes using [synchronization tokens](https://developers.google.com/calendar/v3/sync).


> The first time the run method is called it will perform a full sync and store the sync token. On each subsequent execution it will load the saved sync token and perform an incremental sync.


**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

In the next screencast you can see all `CRUD` operations on the Event BO and how they synchronize with the `Google` Calendar, for both platforms `Win`, `Web` and both directions `Incoming`, `Outgoing`. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used.

<twitter>

[![Xpand XAF Module Office Cloud Google Calendar](https://user-images.githubusercontent.com/159464/94122039-ba0ac080-fe5a-11ea-8723-a973fd1e2852.gif))
](https://youtu.be/kch5gduu0FQ)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/kch5gduu0FQ)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Google.Calendar`.

    The above only references the dependencies and next steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.Google.CalendarModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.Google.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: netstandard2.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.ExpressApp.CodeAnalysis**|**Any**
|Xpand.Extensions|4.221.5
 |Xpand.Extensions.Office.Cloud|4.221.5
 |Xpand.Extensions.Reactive|4.221.5
 |Xpand.Extensions.XAF|4.221.5
 |Xpand.Extensions.XAF.Xpo|4.221.5
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.221.5
 |[Xpand.XAF.Modules.Office.Cloud.Google](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Office.Cloud.Google)|4.221.5
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|5.0.0
 |Google.Apis.Calendar.v3|1.55.0.2410
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.221.5

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Google.Calendar.Office.Office.Cloud.Google.CalendarModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Google.Calendar.Office.Office.Cloud.Google.Calendar). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

