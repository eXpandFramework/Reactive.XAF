![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Google.Tasks.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Google.Tasks.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Google.Tasks.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+label%3AOffice.Cloud.Google.Tasks) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Google.Tasks.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+label%3AOffice.Cloud.Google.Tasks)
# About 

This package integrates with the Google Tasks cloud service.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module that authenticates against the Google Task service using the [Xpand.XAF.Modules.Google](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Google) package, for details head to it's wiki page.

The `Xpand.XAF.Modules.Office.Cloud.Google.Tasks` provides a configurable one way synchronization between the `ITask` Domain Components and the `Google.Apis.Tasks.v1.Data.Task` entity.
All `CRUD` operations will execute the cloud api after a successful XAF transaction. 

* To `configure` the subject `Views` and the target container `Task list` you can use the model.</br>  
  ![image](https://user-images.githubusercontent.com/159464/90819442-aab6c580-e338-11ea-8f9c-6e87f7dc35e9.png)

* To `configure` which CRUD operation will be propagated use the `SynchronizationType` attribute.</br>  
![image](https://user-images.githubusercontent.com/159464/90819817-20bb2c80-e339-11ea-9ed7-3a636753ecda.png)

The package can operate without any configuration by executing a `predefined map` between the `ITask` and `Google.Apis.Tasks.v1.Data.Task` objects on Update and on Insert.

To customize the predefined map you can use a query like the next one which suffix the Google.Apis.Tasks.v1.Data.Task subject with the current date:

```cs

GoogleTasksService.CustomizeSynchronization
    .Do(e => {
        if (e.Instance.mapAction != MapAction.Delete){
            e.Instance.cloud.Title = $"{e.Instance.local.Subject} - {DateTime.Now}";
            e.Handled = true;
        }
    })
    .Subscribe();

```


**Possible future improvements:**

1. Bi-Directional synchronization.
1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

In the next screencast you can see all `CRUD` operations on the Task BO and how they synchronize with the `Google` cloud, for both `Win` and `Web`. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used.

<twitter>

[![Xpand XAF Modules Office Cloud Google Tasks](https://user-images.githubusercontent.com/159464/90682880-56dfaa00-e26e-11ea-981b-d6179572e945.gif)
](https://youtu.be/rxEnuRzY-PA)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/rxEnuRzY-PA)

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Google.Tasks`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.Google.TasksModule));
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
|[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Google.Apis.Auth|1.49.0
 |Google.Apis.Tasks.v1|1.49.0.2049
 |JetBrains.Annotations|2020.1.0
 |Newtonsoft.Json|12.0.3
 |System.Reactive|4.4.1
 |Xpand.Extensions|2.202.50
 |Xpand.Extensions.Office.Cloud|2.202.51.1
 |Xpand.Extensions.Reactive|2.202.51.1
 |Xpand.Extensions.XAF|2.202.51.1
 |Xpand.Extensions.XAF.Xpo|2.202.47
 |[Xpand.XAF.Modules.Office.Cloud.Google](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Office.Cloud.Google)|2.202.12.2
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.51.2
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.10

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Google.Tasks.Office.Office.Cloud.Google.TasksModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Google.Tasks.Office.Office.Cloud.Google.Tasks). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

