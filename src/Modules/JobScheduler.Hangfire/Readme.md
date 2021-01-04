![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.JobScheduler.Hangfire.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.JobScheduler.Hangfire.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/JobScheduler.Hangfire.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+label%3AJobScheduler.Hangfire) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/JobScheduler.Hangfire.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+label%3AJobScheduler.Hangfire)
# About 

The `JobScheduler.Hangfire` package integrates Hangfire for fire & forget job schedules.

## Details

---

**Credits:** to companies that [sponsor](https://github.com/sponsors/apobekiaris) parts of this package.

---

This `Blazor only` module is valuable when you have background process. The JobScheduler module  


To schedule a new Job follow the next steps:
1. Configure the Hangfire [default storage](https://docs.hangfire.io/en/latest/configuration/using-sql-server.html). Additionally consult Hangfire docs to configure/implement any other Hangfire related scenario, there are no restrictions.
1. Add a model assembly `job source` pointing to the assembly containing your Job types.</br>
    ![image](https://user-images.githubusercontent.com/159464/103508193-4b7dcb80-4e69-11eb-82be-7fa109720368.png)
2. Mark any type with a default constructor with the `Xpand.XAF.Modules.JobScheduler.Hangfire.JobProviderAttribute`. 
   ```cs
    [JobProvider]
    public class Job {
    }
   ```
3. All public methods without parameters can be scheduled using the UI or code.
   ```cs
    [JobProvider]
    public class Import {
        // [JobProvider("Customize-Name")]
        public void DailyOrders() {
            throw new NotImplementedException();
        }
    }

    //Schedule the DailyOrders method every day
    var job = objectSpace.CreateObject<Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects.Job>();
    job.JobType = job.JobTypes.First();
    job.JobMethod = job.JobMethods.First();
    job.CronExpression =
        objectSpace.FirstOrDefault<CronExpression>(expression => expression.Name == nameof(Cron.Daily));
    objectSpace.CommitChanges();

    //trigger the new Import().DailyOrder() now
    job.Trigger();

    //pause-resume the DailyOrders
    job.Pause();
    Job.Resume();
   ```
   Note that the methods are scheduled in the background therefore the `HttpContext` is not available.
4. Inject a `BlazorApplication` in the constructor to use its `ServiceProvider` or to query data. 
   ```cs
    [JobProvider]
    public class Import {
        public BlazorApplication Application { get; }
        public Import() { }
        public Import(BlazorApplication application) {
            Application = application;
        }

        // [JobProvider("Customize-Name")]
        public async Task DailyOrders() {
            using var objectSpace = Application.CreateObjectSpace();
            for (int i = 0; i < 10; i++) {
                var order = objectSpace.CreateObject<Order>();
                order.OrderID = i;
            }
            await objectSpace.CommitChangesAsync();
        }
    }
   ```
   Note that the BlazorApplication is not authenticated and the default constructor must exist.
5. Use a Job descendant to pass job specific parameters.
   ```cs
    public class CustomJob:Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects.Job {
        public CustomJob(Session session) : base(session) { }
        string _ordersCount;

        public string OrdersCount {
            get => _ordersCount;
            set => SetPropertyValue(nameof(OrdersCount), ref _ordersCount, value);
        }
    }

    public async Task ImportOrders(PerformContext context) {
        using var objectSpace = Application.CreateObjectSpace();
        var jobId = context.JobId();
        var ordersCount=objectSpace.GetObjectsQuery<CustomJob>()
            .First(job1 => job1.Id == jobId).OrdersCount
        for (int i = 0; i < ordersCount; i++) {
            var order = objectSpace.CreateObject<Order>();
            order.OrderID = i;
        }

        await objectSpace.CommitChangesAsync();
    }

   ```
6. Observe job state.
   ```cs
    Xpand.XAF.Modules.JobScheduler.Hangfire.JobService.JobState.Subscribe(state => {
        var job = state.JobWorker.Job;
    });
   ```

In the screencast you can see `how to declare, schedule, pause, resume and get more details` for a Job.

<twitter>

[![Xpand XAF Modules JobScheduler Hangfire](https://user-images.githubusercontent.com/159464/103535471-6bc57e80-4e99-11eb-9c59-f54b9abb2023.gif)
](https://youtu.be/0LJ2bM1CfMg)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/0LJ2bM1CfMg)

---



**Possible future improvements:**

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.JobScheduler.Hangfire`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.HangfireModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: netstandard2.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
|Xpand.Extensions|4.202.45
 |Xpand.Extensions.Reactive|4.202.45
 |Xpand.Extensions.XAF|4.202.45
 |Xpand.Extensions.XAF.Xpo|4.202.45
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.202.45
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|5.0.0
 |JetBrains.Annotations|2020.3.0
 |Microsoft.CodeAnalysis.CSharp|3.8.0
 |System.Configuration.ConfigurationManager|5.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.11

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobScheduler.HangfireModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/JobScheduler.Hangfire)

