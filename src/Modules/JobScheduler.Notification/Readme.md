![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.JobScheduler.Notification.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.JobScheduler.Notification.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/JobScheduler.Notification.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AJobScheduler.Notification) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/JobScheduler.Notification.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AJobScheduler.Notification)
# About 

The `JobScheduler.Notification` emits Object creation events and offers a Blazor UI for fine tuning them.

## Details

---

**Credits:** to companies that [sponsor](https://github.com/sponsors/apobekiaris) parts of this package.

---

This `Blazor only` module uses the [JobScheduler.Hangfire](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/JobScheduler.Hangfire) module to schedule background processes after certain Business objects are created.

### Configuration
 
Create an `ObjectStateNotification` to configure the notification event emissions. For example the next configuration runs daily and notifies when new Products created.

![image](https://user-images.githubusercontent.com/159464/138347667-5fcb9793-b42f-415e-b65a-92be10d870a6.png)

To subscribe to the above `ObjectStateNotification` events use a snippet like the next one:

```c#
Application.WhenNotification<Product>()
    .Do(t =>  //e.g. create a Task)
    .Finally(() => t.worker.NotifyFinish())//optional line notifies the scheduler to trigger the linked ChainJobs.
    .Subscribe();


```

The package uses `Object member` values to identify the last notification. To configure which member for which type use the model. To create unique indexed member for your objects see the [SequenceGenerator](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/SequenceGenerator) package. if you certain that your data are unique e.g. Product No you can use any integer member as indexer. Members with duplicate values may result in duplicate notifications. 

![image](https://user-images.githubusercontent.com/159464/138351939-6e55c3eb-c66e-415c-a948-641e833315a0.png)

The above `Types` list also populate the `Object` editor in the `ObjectStateNotification` view.

In the screencast:

1. We subscribe to notification with code and create a new `Task` for each new `Product` notification. The subscription is done in the **Blazor** module.
2. We start the `Blazor.Server` and create a new `ObjectStateNotification` for `Product` and schedule it for emitting notifications.
3. We switch to a **Windows** application and create a new `Product`.
4. We switch back to the `Blazor.Server` and manully `trigger `the ObjectStateNotifition `job` instead for wating until it schedules from Hangfire.
5. We switch again to the `Windows` application and open the `Task` listview and see that the task described in the Blazor module code is there.



<twitter tags="#Hangfire.Notification #Blazor">

[![PYlg9dmvsj](https://user-images.githubusercontent.com/159464/138513639-df88c929-3acd-4a63-a75c-4d21f62415c8.gif)](https://youtu.be/sywu43jqV88)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/sywu43jqV88)

---



**Possible future improvements:**

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.JobScheduler.Notification`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.NotificationModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net5.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
 |**DevExpress.Xpo**|**Any**
 |**DevExpress.ExpressApp**|**Any**
 |**DevExpress.ExpressApp.Blazor**|**Any**
 |**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.Validation.Blazor**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.Persistent.Base**|**Any**
|Xpand.Extensions.Blazor|4.211.10
 |Xpand.Extensions.Reactive|4.211.10
 |Xpand.Extensions.XAF|4.211.10
 |Xpand.Extensions|4.211.10
 |Xpand.Extensions.XAF.Xpo|4.211.10
 |[Xpand.XAF.Modules.Blazor](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Blazor)|4.211.10
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.211.10
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |JetBrains.Annotations|2021.2.0
 |System.Reactive|5.0.0
 |System.Interactive|5.0.0
 |Microsoft.CodeAnalysis.CSharp|3.11.0
 |System.Configuration.ConfigurationManager|5.0.0
 |Hangfire.Core|1.7.24
 |System.CodeDom|5.0.0
 |Hangfire.AspNetCore|1.7.24
 |Microsoft.AspNetCore.Hosting.Abstractions|2.2.0
 |Microsoft.Extensions.DependencyInjection.Abstractions|5.0.0
 |Xpand.Patcher|2.0.30
 |Newtonsoft.Json|13.0.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.211.10

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.JobScheduler.Notification.JobScheduler.NotificationModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/JobScheduler.Notification)

